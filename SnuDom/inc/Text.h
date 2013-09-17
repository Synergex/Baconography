#pragma once

#include "DomObject.h"
#include <vector>

#ifdef WP8
namespace SnuDomWP8
#else
namespace SnuDom
#endif
{
	public ref class Text sealed : IDomObject
	{
	internal:
		Text(Platform::String^ plainText, uint32_t domId) 
		{
			Contents = plainText;
			DomID = domId;
		}
	public:
		virtual property uint32_t DomID;
		property bool Italic;
		property int HeaderSize; //0 for no headerness
		property bool Bold;
		property bool Strike;
		property bool Superscript;
		property Platform::String^ Contents;
		virtual void Accept(IDomVisitor^ visitor){ visitor->Visit(this); }
	};
}