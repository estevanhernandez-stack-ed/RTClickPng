#pragma once
#include <array>

// The parent verb's submenu, in display order.  No COM or Windows dependencies, so the C++ test
// runner can pin the order without building any command objects.  Spec: flat list, Settings last.
namespace rtclick {

enum class SubCommand
{
    ConvertToPng,
    CopyAsPng,
    ConvertToJpeg,
    CopyAsJpeg,
    Settings,
};

inline constexpr std::array<SubCommand, 5> kSubCommandOrder = {
    SubCommand::ConvertToPng,
    SubCommand::CopyAsPng,
    SubCommand::ConvertToJpeg,
    SubCommand::CopyAsJpeg,
    SubCommand::Settings,
};

} // namespace rtclick
