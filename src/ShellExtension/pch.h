#pragma once

#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <windows.h>
#include <shellapi.h>
#include <shlobj.h>
#include <shobjidl_core.h>
#include <shlwapi.h>
#include <propkey.h>
#include <propvarutil.h>

#include <wrl/module.h>
#include <wrl/implements.h>
#include <wrl/client.h>

#include <string>
#include <string_view>
#include <vector>
#include <filesystem>
#include <atomic>
#include <optional>
#include <thread>
#include <cstddef>

using Microsoft::WRL::ComPtr;
using Microsoft::WRL::RuntimeClass;
using Microsoft::WRL::RuntimeClassFlags;
using Microsoft::WRL::RuntimeClassType;
using Microsoft::WRL::ClassicCom;

// Module-wide reference counter for DllCanUnloadNow.
extern std::atomic<long> g_dllRefCount;
