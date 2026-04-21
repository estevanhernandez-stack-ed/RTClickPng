#include "pch.h"
#include "Notifier.h"

#include <winrt/base.h>
#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Data.Xml.Dom.h>
#include <winrt/Windows.UI.Notifications.h>

namespace rtclick {

namespace {
    using namespace winrt;
    using namespace winrt::Windows::Foundation;
    using namespace winrt::Windows::Data::Xml::Dom;
    using namespace winrt::Windows::UI::Notifications;

    /// <summary>
    /// Escape for XML text content.  Toast payloads are XML; filenames can contain & or angle brackets.
    /// </summary>
    std::wstring XmlEscape(const std::wstring& s) noexcept
    {
        std::wstring out;
        out.reserve(s.size());
        for (auto c : s)
        {
            switch (c)
            {
                case L'&':  out += L"&amp;"; break;
                case L'<':  out += L"&lt;"; break;
                case L'>':  out += L"&gt;"; break;
                case L'"':  out += L"&quot;"; break;
                default:    out += c; break;
            }
        }
        return out;
    }

    /// <summary>
    /// Post a toast built from a fully-formed XML payload.  Wraps the C++/WinRT calls in a noexcept
    /// boundary — toasts are best-effort, a failure here must never crash Explorer/dllhost.
    /// </summary>
    void ShowToast(const std::wstring& toastXml) noexcept
    {
        try
        {
            XmlDocument doc;
            doc.LoadXml(hstring{toastXml});
            ToastNotification toast{doc};
            // No-arg CreateToastNotifier() picks up the current package's AUMID automatically
            // when running in a packaged context (our DLL is hosted by dllhost under the MSIX
            // package identity via the SurrogateServer registration).
            ToastNotificationManager::CreateToastNotifier().Show(toast);
        }
        catch (...) { /* swallow — toast is cosmetic */ }
    }
}

void Notifier::ConvertSuccess(const std::wstring& destinationPath) noexcept
{
    auto filename = std::filesystem::path(destinationPath).filename().wstring();
    auto folder   = std::filesystem::path(destinationPath).parent_path().wstring();

    std::wstring xml =
        L"<toast><visual><binding template=\"ToastGeneric\">"
        L"<text>Converted to PNG</text>"
        L"<text>" + XmlEscape(filename) + L"</text>"
        L"<text placement=\"attribution\">" + XmlEscape(folder) + L"</text>"
        L"</binding></visual></toast>";
    ShowToast(xml);
}

void Notifier::ConvertBatchSummary(size_t succeeded, size_t failed) noexcept
{
    std::wstring title, body;
    if (failed == 0)
    {
        title = L"Converted " + std::to_wstring(succeeded) + L" files to PNG";
        body  = L"";
    }
    else if (succeeded == 0)
    {
        title = L"Right Click PNG — " + std::to_wstring(failed) + L" errors";
        body  = L"Check source formats or overwrite settings";
    }
    else
    {
        title = L"Converted " + std::to_wstring(succeeded) + L" / " + std::to_wstring(succeeded + failed);
        body  = std::to_wstring(failed) + L" files skipped — check formats or overwrite settings";
    }

    std::wstring xml =
        L"<toast><visual><binding template=\"ToastGeneric\"><text>" + XmlEscape(title) + L"</text>";
    if (!body.empty()) xml += L"<text>" + XmlEscape(body) + L"</text>";
    xml += L"</binding></visual></toast>";
    ShowToast(xml);
}

void Notifier::ConvertError(const std::wstring& sourcePath, const std::wstring& message) noexcept
{
    auto filename = std::filesystem::path(sourcePath).filename().wstring();
    std::wstring xml =
        L"<toast><visual><binding template=\"ToastGeneric\">"
        L"<text>Could not convert " + XmlEscape(filename) + L"</text>"
        L"<text>" + XmlEscape(message) + L"</text>"
        L"</binding></visual></toast>";
    ShowToast(xml);
}

void Notifier::CopyClipboardReady(const std::wstring& sourcePath) noexcept
{
    auto filename = std::filesystem::path(sourcePath).filename().wstring();
    std::wstring xml =
        L"<toast><visual><binding template=\"ToastGeneric\">"
        L"<text>Copied as PNG — ready to paste</text>"
        L"<text>" + XmlEscape(filename) + L"</text>"
        L"</binding></visual></toast>";
    ShowToast(xml);
}

} // namespace rtclick
