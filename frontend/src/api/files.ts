export interface FileItem {
  id: string;
  name: string;
  path: string;
  isDirectory: boolean;
  size: number;
  lastModified: string;
}

// API base URL
const API_ROOT = "/api/files";

export async function fetchFiles(directoryPath = "/"): Promise<FileItem[]> {
  const res = await fetch(`${API_ROOT}?directoryPath=${encodeURIComponent(directoryPath)}`);
  if (!res.ok) throw new Error("Failed to fetch files");
  return res.json();
}

export async function createFile(directoryPath: string, fileName: string): Promise<FileItem> {
  const res = await fetch(`${API_ROOT}?directoryPath=${encodeURIComponent(directoryPath)}&fileName=${encodeURIComponent(fileName)}`, {
    method: "POST",
  });
  if (!res.ok) throw new Error("Failed to create file");
  return res.json();
}