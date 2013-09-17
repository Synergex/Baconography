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

	public enum class ColumnAlignment
	{
		Left,
		Right,
		Center
	};

	public ref class TableColumn sealed : IDomObject
	{
	internal:
		TableColumn(std::vector<IDomObject^> children, uint32_t domId, int flags);
	public:
		virtual property uint32_t DomID;
		property Windows::Foundation::Collections::IIterable<IDomObject^>^ Contents;
		property ColumnAlignment Alignment;
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

	TableColumn::TableColumn(std::vector<IDomObject^> children, uint32_t domId, int flags) 
	{
		DomID = domId;
		Contents = ref new Platform::Collections::Vector<IDomObject^>(children);
		switch(flags & MKD_TABLE_ALIGNMASK)
		{
		case MKD_TABLE_ALIGN_L:
			Alignment = ColumnAlignment::Left;
			break;
		case MKD_TABLE_ALIGN_R:
			Alignment = ColumnAlignment::Right;
			break;
		case MKD_TABLE_ALIGN_CENTER:
			Alignment = ColumnAlignment::Center;
			break;
		}
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