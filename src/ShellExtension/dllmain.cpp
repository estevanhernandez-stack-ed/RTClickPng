#include "pch.h"
#include "ConvertToPngCommand.h"

using namespace rtclick;

std::atomic<long> g_dllRefCount = 0;
HMODULE g_hModule = nullptr;

// Classic COM class factory — one template instantiation per IExplorerCommand CLSID.
template <typename TCommand>
class CommandClassFactory :
    public RuntimeClass<RuntimeClassFlags<ClassicCom>, IClassFactory>
{
public:
    IFACEMETHODIMP CreateInstance(IUnknown* outer, REFIID riid, void** out) noexcept override
    {
        *out = nullptr;
        if (outer) return CLASS_E_NOAGGREGATION;
        auto cmd = Microsoft::WRL::Make<TCommand>();
        if (!cmd) return E_OUTOFMEMORY;
        return cmd->QueryInterface(riid, out);
    }
    IFACEMETHODIMP LockServer(BOOL lock) noexcept override
    {
        if (lock) ++g_dllRefCount;
        else      --g_dllRefCount;
        return S_OK;
    }
};

extern "C" BOOL WINAPI DllMain(HMODULE hModule, DWORD reason, LPVOID)
{
    if (reason == DLL_PROCESS_ATTACH)
    {
        g_hModule = hModule;
        DisableThreadLibraryCalls(hModule);
    }
    return TRUE;
}

extern "C" HRESULT WINAPI DllGetClassObject(REFCLSID clsid, REFIID riid, LPVOID* ppv)
{
    if (!ppv) return E_POINTER;
    *ppv = nullptr;

    if (IsEqualCLSID(clsid, CLSID_ConvertToPngCommand))
    {
        auto f = Microsoft::WRL::Make<CommandClassFactory<ConvertToPngCommand>>();
        return f ? f->QueryInterface(riid, ppv) : E_OUTOFMEMORY;
    }
    if (IsEqualCLSID(clsid, CLSID_CopyAsPngCommand))
    {
        auto f = Microsoft::WRL::Make<CommandClassFactory<CopyAsPngCommand>>();
        return f ? f->QueryInterface(riid, ppv) : E_OUTOFMEMORY;
    }
    if (IsEqualCLSID(clsid, CLSID_ConvertToJpegCommand))
    {
        auto f = Microsoft::WRL::Make<CommandClassFactory<ConvertToJpegCommand>>();
        return f ? f->QueryInterface(riid, ppv) : E_OUTOFMEMORY;
    }
    if (IsEqualCLSID(clsid, CLSID_CopyAsJpegCommand))
    {
        auto f = Microsoft::WRL::Make<CommandClassFactory<CopyAsJpegCommand>>();
        return f ? f->QueryInterface(riid, ppv) : E_OUTOFMEMORY;
    }
    if (IsEqualCLSID(clsid, CLSID_OpenSettingsCommand))
    {
        auto f = Microsoft::WRL::Make<CommandClassFactory<OpenSettingsCommand>>();
        return f ? f->QueryInterface(riid, ppv) : E_OUTOFMEMORY;
    }
    return CLASS_E_CLASSNOTAVAILABLE;
}

extern "C" HRESULT WINAPI DllCanUnloadNow()
{
    return g_dllRefCount.load() == 0 ? S_OK : S_FALSE;
}
