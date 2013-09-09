#pragma once

#include "DomObject.h"
#include "Paragraph.h"

#include <vector>

#ifdef WP8
namespace SnuDomWP8
#else
namespace SnuDom
#endif
{
	ref class TableColumn;
	ref class Table;

	public ref class TableRow sealed : IDomObject
	{
	internal:
		TableRow(std::vector<IDomObject^> children, uint32_t domId);

	public:
		virtual property uint32_t DomID;
		property Windows::Foundation::Collections::IIterable<TableColumn^>^ Columns;
		virtual void Accept(IDomVisitor^ visitor) { visitor->Visit(this); }
	};

	public ref class TableColumn sealed : IDomObject
	{
	internal:
		TableColumn(std::vector<IDomObject^> children, uint32_t domId);
	public:
		virtual property uint32_t DomID;
		property Windows::Foundation::Collections::IIterable<IDomObject^>^ Contents;
		virtual void Accept(IDomVisitor^ visitor) { visitor->Visit(this); }
	};

	public ref class Table sealed : IDomObject
	{
	internal:
		Table(std::vector<IDomObject^> header, std::vector<IDomObject^> children, uint32_t domId) 
		{
			DomID = domId;
			if(header.size() == 1)
			{
				auto headerRow = dynamic_cast<TableRow^>(header[0]);
				if(headerRow != nullptr)
				{
					Headers = headerRow->Columns;
				}
			}

			auto rows = ref new Platform::Collections::Vector<TableRow^>();
			for(auto obj : children)
			{
				auto objRow = dynamic_cast<TableRow^>(obj);
				if(objRow != nullptr)
					rows->Append(objRow);
			}
			Rows = rows;
		}
	public:
		virtual property uint32_t DomID;
		property Windows::Foundation::Collections::IIterable<TableRow^>^ Rows;
		property Windows::Foundation::Collections::IIterable<TableColumn^>^ Headers;
		virtual void Accept(IDomVisitor^ visitor) { visitor->Visit(this); }
	};

	TableColumn::TableColumn(std::vector<IDomObject^> children, uint32_t domId) 
	{
		DomID = domId;
		Contents = ref new Platform::Collections::Vector<IDomObject^>(children);
	}

	TableRow::TableRow(std::vector<IDomObject^> children, uint32_t domId) 
	{
		DomID = domId;
		auto columns = ref new Platform::Collections::Vector<TableColumn^>();
		for(auto obj : children)
		{
			auto objCol = dynamic_cast<TableColumn^>(obj);
			if(objCol != nullptr)
				columns->Append(objCol);
		}
		Columns = columns;
	}
}