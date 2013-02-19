#pragma once

#ifndef WP8
namespace SoldOutW8
{
public ref class SoldOut sealed
#else
namespace SoldOutWP8
{
public ref class SoldOut sealed
#endif

    {
    public:
        static Platform::String^ MarkdownToXaml(Platform::String^ source);
    };
}