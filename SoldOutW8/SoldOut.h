#pragma once
#include <cstdint>
#if defined( _WIN64 ) // Compile time.
	typedef uint64 VarPtr;
#else
	typedef uint32 VarPtr;
#endif

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
        static VarPtr MarkdownToXaml(VarPtr source, std::uint32_t sourceLength);
    };
}