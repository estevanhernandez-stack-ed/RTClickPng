#include "pch.h"
#include "ConvertToPngCommand.h"

using namespace rtclick;

std::atomic<long> g_dllRefCount = 0;
HMODULE g_hModule = nullptr;

// Classic COM class factory — one per IExplorerCommand CLSID.  We switch on the requested
// CLSID in DllGetClassObject and produce the matching factory on demand.
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
        auto factory = Microsoft::WRL::Make<CommandClassFactory<ConvertToPngCommand>>();
        if (!factory) return E_OUTOFMEMORY;
        return factory->QueryInterface(riid, ppv);
    }
    return CLASS_E_CLASSNOTAVAILABLE;
}

extern "C" HRESULT WINAPI DllCanUnloadNow()
{
    return g_dllRefCount.load() == 0 ? S_OK : S_FALSE;
}
