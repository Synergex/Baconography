#include "DomObject.h"


#ifdef WP8
namespace SnuDomWP8
#else
namespace SnuDom
#endif
{
	public ref class Code sealed : IDomContainer
	{
	private:
		Platform::Collections::Vector<IDomObject^> _children;
	internal:
		Code(std::vector<IDomObject^> children, uint32_t domId) : _children(children)
		{
			DomID = domId;
		}
	public:
		virtual property uint32_t DomID;
		property bool IsBlock;
		virtual Windows::Foundation::Collections::IIterator<IDomObject^>^ First(){return _children.First();}
		virtual void Accept(IDomVisitor^ visitor){ visitor->Visit(this); }
	};
}