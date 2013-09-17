#pragma once

#include "DomObject.h"
#include "Text.h"
#include "vector"
#ifdef WP8
namespace SnuDomWP8
#else
namespace SnuDom
#endif
{
	public ref class Link sealed : IDomObject
	{
	internal:
		Link(Platform::String^ link, Platform::String^ title, std::vector<IDomObject^>& expandedDisplay, uint32_t domId)
		{
			DomID = domId;
			Url = link;
			if(title != nullptr)
				Hover = ref new Text(title, 0);
			Display = ref new Platform::Collections::Vector<IDomObject^>(expandedDisplay);
		}
	public:
		virtual property uint32_t DomID;
		property Windows::Foundation::Collections::IIterable<IDomObject^>^ Display;
		property Text^ Hover;
		property Platform::String^ Url;

		virtual void Accept(IDomVisitor^ visitor){ visitor->Visit(this);}
	};
}