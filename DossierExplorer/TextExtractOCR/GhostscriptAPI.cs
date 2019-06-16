using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.IO;
using System.Web;
using System.Reflection;

namespace DossierExplorer
{
    class GhostscriptAPI
    {
 public static void  CopyGhostScriptDll()//copy the dll file for ghostscript if app x64 replace with 64bit dll else replace with 32bit
        {
            string binPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string gsDllPath = Path.Combine(binPath, Environment.Is64BitProcess ? "gsdll64.dll" : "gsdll32.dll");

               
           
                File.Copy(Path.Combine(GlobalVariables.MyAppPath,"DLLs", Environment.Is64BitProcess ? "gsdll64.dll" : "gsdll32.dll"), Path.Combine(binPath, "gsdll.dll"), true);
            
           
        }

       
        // Import GS Dll
        [DllImport("gsdll.dll")]
        private static extern int gsapi_new_instance(out IntPtr pinstance, IntPtr caller_handle);

        [DllImport("gsdll.dll")]
        private static extern int gsapi_init_with_args(IntPtr instance, int argc, IntPtr argv);

        [DllImport("gsdll.dll")]
        private static extern int gsapi_exit(IntPtr instance);

        [DllImport("gsdll.dll")]
        private static extern void gsapi_delete_instance(IntPtr instance);

        // Set variables to be used in the class
        private ArrayList _gsParams = new ArrayList();
        private IntPtr _gsInstancePtr;
        private GCHandle[] _gsArgStrHandles = null;
        private IntPtr[] _gsArgPtrs = null;
        private GCHandle _gsArgPtrsHandle;

        public GhostscriptAPI() {
          
        }
        public GhostscriptAPI(string[] Params)
        {
           
            _gsParams.AddRange(Params);
            Execute();
        }

        public string[] Params
        {
            get { return (string[])_gsParams.ToArray(typeof(string)); }
        }

        public void AddParam(string Param) { _gsParams.Add(Param); }
        public void RemoveParamAtIndex(int Index) { _gsParams.RemoveAt(Index); }
        public void RemoveParam(string Param) { _gsParams.Remove(Param); }

        public void Execute()
        {
            // Create GS Instance (GS-API)
            gsapi_new_instance(out _gsInstancePtr, IntPtr.Zero);
            // Build Argument Arrays
            _gsArgStrHandles = new GCHandle[_gsParams.Count];
            _gsArgPtrs = new IntPtr[_gsParams.Count];

            // Populate Argument Arrays
            for (int i = 0; i < _gsParams.Count; i++)
            {
                _gsArgStrHandles[i] = GCHandle.Alloc(System.Text.Encoding.UTF8.GetBytes(_gsParams[i].ToString()), GCHandleType.Pinned);
                _gsArgPtrs[i] = _gsArgStrHandles[i].AddrOfPinnedObject();
            }

            // Allocate memory that is protected from Garbage Collection
            _gsArgPtrsHandle = GCHandle.Alloc(_gsArgPtrs, GCHandleType.Pinned);
            // Init args with GS instance (GS-API)
            gsapi_init_with_args(_gsInstancePtr, _gsArgStrHandles.Length, _gsArgPtrsHandle.AddrOfPinnedObject());
            // Free unmanaged memory
            for (int i = 0; i < _gsArgStrHandles.Length; i++)
                _gsArgStrHandles[i].Free();
            _gsArgPtrsHandle.Free();

            // Exit the api (GS-API)
            gsapi_exit(_gsInstancePtr);
            // Delete GS Instance (GS-API)
            gsapi_delete_instance(_gsInstancePtr);
        }
    }
}
