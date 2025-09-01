import { useEffect, useState } from "react";
import { FileItem, fetchFiles, createFile } from "../api/files";

export default function FileList() {
  const [files, setFiles] = useState<FileItem[]>([]);
  const [directoryPath] = useState("/");
  const [newFileName, setNewFileName] = useState("");

  useEffect(() => {
    fetchFiles(directoryPath).then(setFiles);
  }, [directoryPath]);

  const handleCreate = async () => {
    if (!newFileName) return;
    const file = await createFile(directoryPath, newFileName);
    setFiles(f => [...f, file]);
    setNewFileName("");
  };

  return (
    <div>
      <h2>Files in {directoryPath}</h2>
      <ul>
        {files.map(f => (
          <li key={f.id}>
            {f.isDirectory ? "📁" : "📄"} {f.name}
          </li>
        ))}
      </ul>
      <input
        value={newFileName}
        onChange={e => setNewFileName(e.target.value)}
        placeholder="New file name"
      />
      <button onClick={handleCreate}>Create File</button>
    </div>
  );
}