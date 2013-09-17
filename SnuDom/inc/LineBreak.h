#pragma once

#include "DomObject.h"


#ifdef WP8
namespace SnuDomWP8
#else
namespace SnuDom
#endif
{
	public ref class LineBreak sealed : IDomObject
	{
	public:
		virtual property uint32_t DomID;
		LineBreak(uint32_t domId) {DomID = domId;}
		virtual void Accept(IDomVisitor^ visitor){ visitor->Visit(this); }
	};
}