using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.System.File;
using FFXIVClientStructs.FFXIV.Client.System.Resource;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using InteropGenerator.Runtime;
using Lumina;
using Penumbra.String;
using Penumbra.String.Classes;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using VfxEditor.Select;
using VfxEditor.Structs;
using FileMode = FFXIVClientStructs.FFXIV.Client.System.File.FileMode;

namespace VfxEditor.Interop {
    public unsafe partial class ResourceLoader {
#nullable enable
        private event Action<ResourceType, FullPath?>? PathResolved;
#nullable disable

        // ===== FILES =========

        public delegate byte ReadFilePrototype( IntPtr fileHandler, FileDescriptor* fileDesc, int priority, bool isSync );

        public delegate byte ReadSqpackPrototype( IntPtr fileHandler, FileDescriptor* fileDesc, int priority, bool isSync );

        public delegate ResourceHandle* GetResourceSyncPrototype( ResourceManager* resourceManager, ResourceCategory* category, uint* type, uint* hash, CStringPointer path,
            void* unknown, void* unkDebugPtr, uint unkDebugInt );

        public delegate ResourceHandle* GetResourceAsyncPrototype( ResourceManager* resourceManager, ResourceCategory* category, uint* type, uint* hash, CStringPointer path,
            void* unknown, bool isUnknown, void* unkDebugPtr, uint unkDebugInt );

        // ====== FILES HOOKS ========

        public Hook<GetResourceSyncPrototype> GetResourceSyncHook { get; private set; }

        public Hook<GetResourceAsyncPrototype> GetResourceAsyncHook { get; private set; }

        public Hook<ReadSqpackPrototype> ReadSqpackHook { get; private set; }

        public ReadFilePrototype ReadFile { get; private set; }

        private ResourceHandle* GetResourceSyncDetour(
            ResourceManager* resourceManager, ResourceCategory* category, uint* type, uint* hash, CStringPointer path,
            void* unknown, void* unkDebugPtr, uint unkDebugInt
        ) => GetResourceHandler( true, resourceManager, category, type, hash, path, unknown, false, unkDebugPtr, unkDebugInt );

        private ResourceHandle* GetResourceAsyncDetour(
            ResourceManager* resourceManager, ResourceCategory* category, uint* type, uint* hash, CStringPointer path,
            void* unknown, bool isUnknown, void* unkDebugPtr, uint unkDebugInt
        ) => GetResourceHandler( false, resourceManager, category, type, hash, path, unknown, isUnknown, unkDebugPtr, unkDebugInt );

        private ResourceHandle* CallOriginalHandler(
            bool isSync,
            ResourceManager* resourceManager, ResourceCategory* category, uint* type, uint* hash, CStringPointer path,
            void* unknown, bool isUnknown, void* unkDebugPtr, uint unkDebugInt
        ) => isSync
            ? GetResourceSyncHook.Original( resourceManager, category, type, hash, path, unknown, unkDebugPtr, unkDebugInt )
            : GetResourceAsyncHook.Original( resourceManager, category, type, hash, path, unknown, isUnknown, unkDebugPtr, unkDebugInt );

        private ResourceHandle* GetResourceHandler(
            bool isSync,
            ResourceManager* resourceManager, ResourceCategory* category, uint* type, uint* hash, CStringPointer path,
            void* unknown, bool isUnknown, void* unkDebugPtr, uint unkDebugInt
        ) {
            if( !Utf8GamePath.FromPointer( path, MetaDataComputation.None, out var gamePath ) ) {
                return CallOriginalHandler( isSync, resourceManager, category, type, hash, path, unknown, isUnknown, unkDebugPtr, unkDebugInt );
            }

            var gamePathString = gamePath.ToString();

            if( Plugin.Configuration?.LogAllFiles == true ) {
                Dalamud.Log( $"[GetResourceHandler] {gamePathString}" );
                if( SelectDialog.LoggedFiles.Count > 1000 ) SelectDialog.LoggedFiles.Clear();
                SelectDialog.LoggedFiles.Add( gamePathString );
            }

            var replacedPath = GetReplacePath( gamePathString, out var localPath ) ? localPath : null;

            if( replacedPath == null || replacedPath.Length >= 260 ) {
                var unreplaced = CallOriginalHandler( isSync, resourceManager, category, type, hash, path, unknown, isUnknown, unkDebugPtr, unkDebugInt );
                if( Plugin.Configuration?.LogDebug == true && DoDebug( gamePathString ) ) Dalamud.Log( $"[GetResourceHandler] ORIGINAL: {gamePathString} -> " + new IntPtr( unreplaced ).ToString( "X8" ) );
                return unreplaced;
            }

            var resolvedPath = new FullPath( replacedPath );
            PathResolved?.Invoke( (ResourceType)(*type), resolvedPath );

            *hash = ( uint )InteropUtils.ComputeHash( resolvedPath.InternalName, ( GetResourceParameters* )unknown  ) ;
            path = resolvedPath.InternalName.Path;

            var replaced = CallOriginalHandler( isSync, resourceManager, category, type, hash, path, unknown, isUnknown, unkDebugPtr, unkDebugInt );
            if( Plugin.Configuration?.LogDebug == true ) Dalamud.Log( $"[GetResourceHandler] REPLACED: {gamePathString} -> {replacedPath} -> " + new IntPtr( replaced ).ToString( "X8" ) );
            return replaced;
        }

        private byte ReadSqpackDetour( IntPtr fileHandler, FileDescriptor* fileDesc, int priority, bool isSync ) {
            if( fileDesc->ResourceHandle == null ) return ReadSqpackHook.Original( fileHandler, fileDesc, priority, isSync );

            if( !Utf8GamePath.FromSpan( fileDesc->ResourceHandle->FileName.AsSpan(), MetaDataComputation.All, out var originalGamePath ) ) {
                return ReadSqpackHook.Original( fileHandler, fileDesc, priority, isSync );
            }

            var originalPath = originalGamePath.ToString();
            var isPenumbra = ProcessPenumbraPath( originalPath, out var actualPath );

            if( Plugin.Configuration?.LogDebug == true ) Dalamud.Log( $"[ReadSqpackHandler] {actualPath}" );

            var isRooted = Path.IsPathRooted( actualPath );

            // looking for refreshed paths, could also be like |default_1|path.avfx
            if( actualPath != null && !isRooted ) {
                var replacementPath = GetReplacePath( actualPath, out var localPath ) ? localPath : null;
                if( replacementPath != null && Path.IsPathRooted( replacementPath ) && replacementPath.Length < 260 ) {
                    actualPath = replacementPath;
                    isRooted = true;
                    isPenumbra = false;
                }
            }

            // call the original if it's a penumbra path that doesn't need replacement as well
            if( actualPath == null || actualPath.Length >= 260 || !isRooted || isPenumbra ) {
                if( Plugin.Configuration?.LogDebug == true ) Dalamud.Log( $"[ReadSqpackHandler] ORIGINAL: {originalPath}" );
                return ReadSqpackHook.Original( fileHandler, fileDesc, priority, isSync );
            }

            if( Plugin.Configuration?.LogDebug == true ) Dalamud.Log( $"[ReadSqpackHandler] REPLACED: {actualPath}" );

            fileDesc->FileMode = FileMode.LoadUnpackedResource;
            ByteString.FromString( actualPath, out var gamePath );

            // note: must be utf16
            var utfPath = Encoding.Unicode.GetBytes( actualPath );
            Marshal.Copy( utfPath, 0, (nint)fileDesc + 0x70, utfPath.Length );
            var fi = stackalloc byte[0x20 + utfPath.Length + 0x16];
            Marshal.Copy( utfPath, 0, (nint)fi + 0x21, utfPath.Length );
            fileDesc->FileInterface = ( FileInterface* )fi ;

            return ReadFile( fileHandler, fileDesc, priority, isSync );

            /*var path = CiByteString.FromSpanUnsafe( Encoding.Unicode.GetBytes( actualPath ),
                originalGamePath.Path.IsNullTerminated,
                originalGamePath.Path.IsAsciiLowerCase,
                originalGamePath.Path.IsAscii
            );

            Marshal.WriteIntPtr( (int)fileDesc->ResourceHandle + 0x48, (nint)path.Path );
            fileDesc->ResourceHandle->FileName.Length = ( ulong )path.Length;

            fileDesc->FileMode = FileMode.LoadUnpackedResource;
            var fi = stackalloc char[0x11 + 0x0B + 14];
            var fileInterface = ( FileInterface* )fi + 1;
            fileDesc->FileInterface = fileInterface;
            InteropUtils.WritePtr( fi + 0x11, path.Path, path.Length );
            InteropUtils.WritePtr( (char*)((nint)fileDesc + 0x70), path.Path, path.Length );

            var ret = ReadFile( fileHandler, fileDesc, priority, isSync );

            // Reset path
            Marshal.WriteIntPtr( ( int )fileDesc->ResourceHandle + 0x48, ( nint )originalGamePath.Path.Path );
            fileDesc->ResourceHandle->FileName.Length = ( ulong )originalGamePath.Path.Length;

            return ret;*/

            /*
             * var path = CiByteString.FromSpanUnsafe(actualPath, gamePath.Path.IsNullTerminated, gamePath.Path.IsAsciiLowerCase,
            gamePath.Path.IsAscii);
            fileDescriptor->ResourceHandle->FileNameData   = path.Path;
            fileDescriptor->ResourceHandle->FileNameLength = path.Length;
            PreLoadFile?.Invoke(fileDescriptor->ResourceHandle, path, data);
            returnValue = DefaultLoadResource(path, fileDescriptor, priority, isSync, data);
            // Return original resource handle path so that they can be loaded separately.
            fileDescriptor->ResourceHandle->FileNameData   = gamePath.Path.Path;
            fileDescriptor->ResourceHandle->FileNameLength = gamePath.Path.Length;



             // Specify that we are loading unpacked files from the drive.
            // We need to obtain the actual file path in UTF16 (Windows-Unicode) on two locations,
            // but we write a pointer to the given string instead and use the CreateFileW hook to handle it,
            // because otherwise we are limited to 260 characters.
            fileDescriptor->FileMode = FileMode.LoadUnpackedResource;

            // Ensure that the file descriptor has its wchar_t array on aligned boundary even if it has to be odd.
            var fd = stackalloc char[0x11 + 0x0B + 14];
            fileDescriptor->FileDescriptor = (byte*)fd + 1;
            CreateFileWHook.WritePtr(fd + 0x11,                      gamePath.Path, gamePath.Length);
            CreateFileWHook.WritePtr(&fileDescriptor->Utf16FileName, gamePath.Path, gamePath.Length);

            // Use the SE ReadFile function.
            var ret = _fileReadService.ReadFile(fileDescriptor, priority, isSync);
            FileLoaded?.Invoke(fileDescriptor->ResourceHandle, gamePath, ret != 0, true, additionalData);
            return ret;








            fileDesc->FileMode = FileMode.LoadUnpackedResource;

            ByteString.FromString( gameFsPath, out var gamePath );

            // note: must be utf16
            var utfPath = Encoding.Unicode.GetBytes( gameFsPath );
            Marshal.Copy( utfPath, 0, new IntPtr( &fileDesc->Utf16FileName ), utfPath.Length );
            var fd = stackalloc byte[0x20 + utfPath.Length + 0x16];
            Marshal.Copy( utfPath, 0, new IntPtr( fd + 0x21 ), utfPath.Length );
            fileDesc->FileDescriptor = fd;

            return ReadFile( fileHandler, fileDesc, priority, isSync );
            */
        }
    }
}
