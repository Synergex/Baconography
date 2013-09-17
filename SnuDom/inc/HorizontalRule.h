#pragma once

#include "DomObject.h"


#ifdef WP8
namespace SnuDomWP8
#else
namespace SnuDom
#endif
{
	public ref class HorizontalRule sealed : IDomObject
	{
	public:
		virtual property uint32_t DomID;
		HorizontalRule(uint32_t domId) {DomID = domId;}
		virtual void Accept(IDomVisitor^ visitor){ visitor->Visit(this); }
	};
}