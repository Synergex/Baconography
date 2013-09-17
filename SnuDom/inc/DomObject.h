#pragma once

#include <collection.h>
#include <windows.foundation.h>

#ifdef WP8
namespace SnuDomWP8
#else
namespace SnuDom
#endif
{
	ref class Text;
	ref class Code;
	ref class Quote;
	ref class OrderedList;
	ref class UnorderedList;
	ref class HorizontalRule;
	ref class Table;
	ref class Link;
	ref class Paragraph;
	ref class Document;
	ref class TableRow;
	ref class TableColumn;
	ref class LineBreak;

	interface class IDomVisitor;

	public interface class IDomObject
	{
	public:
		void Accept(IDomVisitor^ visitor);
		property uint32_t DomID;
	};

	public interface class IDomContainer : Windows::Foundation::Collections::IIterable<IDomObject^>, IDomObject
	{
	};

	public interface class IDomVisitor
	{
	public:
		void Visit(Text^ text);
		void Visit(Code^ code);
		void Visit(Quote^ quote);
		void Visit(OrderedList^ orderedList);
		void Visit(UnorderedList^ unorderedList);
		void Visit(HorizontalRule^ horizontalRule);
		void Visit(Table^ table);
		void Visit(Link^ link);
		void Visit(Paragraph^ paragraph);
		void Visit(Document^ document);
		void Visit(TableRow^ tableRow);
		void Visit(TableColumn^ tableColumn);
		void Visit(LineBreak^ lineBreak);
	};

	public ref class Document sealed : IDomContainer
	{
	private:
		Platform::Collections::Vector<IDomObject^> _children;
	internal:
		Document(std::vector<IDomObject^> children) : _children(children)
		{
			DomID = 0;
		}
	public:
		virtual property uint32_t DomID;
		virtual void Accept(IDomVisitor^ visitor){ visitor->Visit(this); }
		virtual Windows::Foundation::Collections::IIterator<IDomObject^>^ First(){return _children.First();}
		
	};
}