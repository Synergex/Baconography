#pragma once

#include "DomObject.h"

#include <vector>

#ifdef WP8
namespace SnuDomWP8
#else
namespace SnuDom
#endif
{
	public ref class Quote sealed : IDomContainer
	{
	private:
		Platform::Collections::Vector<IDomObject^> _children;
	internal:
		Quote(std::vector<IDomObject^> children, uint32_t domId) : _children(children)
		{
			DomID = domId;
		}
	public:
		virtual property uint32_t DomID;
		virtual Windows::Foundation::Collections::IIterator<IDomObject^>^ First(){return _children.First();}
		virtual void Accept(IDomVisitor^ visitor){ visitor->Visit(this); }
	};
}