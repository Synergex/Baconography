#include "markdown.h"
#include <collection.h>
#include <vector>
#include <list>
#include <map>
#include <array>
#include <Windows.Foundation.h>
#include "DomObject.h"
#include "Link.h"
#include "Code.h"
#include "Quote.h"
#include "HorizontalRule.h"
#include "LineBreak.h"
#include "OrderedList.h"
#include "UnorderedList.h"
#include "Paragraph.h"
#include "Table.h"
#include "SimpleSessionMemoryPool.h"

using std::vector;
using std::list;
using std::array;
using std::map;

#ifdef WP8
namespace SnuDomWP8
#else
namespace SnuDom
#endif
{
	void* rndr_allocate(void *opaque, size_t size);

	struct dom_builder_state
	{
		dom_builder_state() : domId(1) {}
		uint32_t domId;
		map<uint32_t, IDomObject^> unclaimedDomIdMap;
		SimpleSessionMemoryPool memoryPool;
	};

	Platform::String^ toPlatformString(const char* src, uint32_t sourceLength)
	{
		if(src == nullptr || sourceLength == 0)
			return nullptr;

		wchar_t* buffer = (wchar_t*)_alloca(sourceLength * 2);
		auto len = MultiByteToWideChar(CP_UTF8, 0, src, sourceLength, buffer, sourceLength * 2);
		return ref new Platform::String( buffer, len);
	}

	Platform::String^ toPlatformString(const struct buf * buffer)
	{
		if(buffer == nullptr || buffer->size == 0)
			return nullptr;

		return toPlatformString((const char*)buffer->data, buffer->size);
	}

	void makeDomId(struct buf* ob, int domId, void* opaque)
	{
		array<uint8_t, 6> result;
		memset(&result[0], 4, 6);
		result[0] = 3;
		auto itoaLen = strlen(_itoa(domId, (char*)&result[1], 10));
		result[itoaLen + 1] = 4;
		bufput(opaque, rndr_allocate, ob, (const char*)&result[0], 6);
	}

	uint32_t nextId(const struct buf * text, ptrdiff_t& offset, Platform::String^& plainText)
	{
		for(size_t i = offset; i < text->size;i++)
		{
			//start sentinal
			if(text->data[i] == 3)
			{
				if(i - offset > 0)
				{
					plainText = toPlatformString((const char*)text->data + offset, i - offset);

				}
				
				auto end = i;
				for(;end - i < 5 && end < text->size && text->data[end] != 4;end++);

				std::array<uint8_t, 6> atoiBuff;
				memset(&atoiBuff[0], 0, 6);
				memcpy(&atoiBuff[0], text->data + i + 1, end - i - 1);
				offset = i + 6;
				return atoi((const char*)&atoiBuff[0]);
			}
		}
		plainText = nullptr;
		return 0;
	}

	//this is for anywhere we are making new Text objects, 
	void splat_text(const struct buf * text, dom_builder_state* state, vector<uint32_t>& splatIds)
	{
		//find naked text between ids
		ptrdiff_t offset = 0;
		vector<uint32_t> bufferIds;
		while(offset < text->size)
		{
			Platform::String^ plainText = nullptr;
			auto foundId = nextId(text, offset, plainText);
			if(plainText != nullptr)
			{
				auto newDomId = state->domId++;
				state->unclaimedDomIdMap[newDomId] = ref new Text(plainText, newDomId);
				splatIds.push_back(newDomId);
			}

			if(foundId != 0)
				splatIds.push_back(foundId);
			else
			{
				auto newDomId = state->domId++;
				state->unclaimedDomIdMap[newDomId] = ref new Text(toPlatformString((const char*)text->data + offset, text->size - offset), newDomId);
				splatIds.push_back(newDomId);
				break;
			}
		}
	}

	void consume_text(const struct buf* text, dom_builder_state* state, vector<IDomObject^>& expanded)
	{
		//find naked text between ids
		ptrdiff_t offset = 0;
		vector<uint32_t> bufferIds;
		while(offset < text->size)
		{
			Platform::String^ plainText = nullptr;
			auto foundId = nextId(text, offset, plainText);
			if(plainText != nullptr)
			{
				auto newDomId = state->domId++;
				expanded.push_back(ref new Text(plainText, newDomId));
			}

			if(foundId != 0)
			{
				auto findItr = state->unclaimedDomIdMap.find(foundId);
				expanded.push_back(findItr->second);
				state->unclaimedDomIdMap.erase(findItr);
			}
			else
			{
				auto newDomId = state->domId++;
				expanded.push_back(ref new Text(toPlatformString((const char*)text->data + offset, text->size - offset), newDomId));
				break;
			}
		}
	}

	static int rndr_autolink(struct buf *ob, const struct buf *link, enum mkd_autolink type, void *opaque) 
	{
		
		if (!link || !link->size) return 0;

		auto state = static_cast<dom_builder_state*>(opaque);
		//children should not have any elements
		auto newDomId = state->domId++;
		auto result = ref new Link(toPlatformString(link), nullptr, vector<IDomObject^>(), newDomId);
		state->unclaimedDomIdMap[newDomId] = result;
		makeDomId(ob, newDomId, opaque);
		return 1; 
	}

	static void rndr_blockcode(struct buf *ob, const struct buf *text, const struct buf *lang, void *opaque) 
	{
		//we should be looking at zero or more processed children here
		auto state = static_cast<dom_builder_state*>(opaque);
		vector<IDomObject^> expanded;
		consume_text(text, state, expanded);
		auto newDomId = state->domId++;
		auto result = ref new Code(expanded, newDomId);
		result->IsBlock = true;
		state->unclaimedDomIdMap[newDomId] = result;
		makeDomId(ob, newDomId, opaque);
	}

	static void rndr_blockquote(struct buf *ob, const struct buf *text, void *opaque) 
	{
		//we should be looking at zero or more processed children here
		auto state = static_cast<dom_builder_state*>(opaque);
		vector<IDomObject^> expanded;
		consume_text(text, state, expanded);
		auto newDomId = state->domId++;
		auto result = ref new Quote(expanded, state->domId);
		state->unclaimedDomIdMap[newDomId] = result;
		makeDomId(ob, newDomId, opaque);
	}

	static int rndr_codespan(struct buf *ob,const  struct buf *text, void *opaque) 
	{
		//we should be looking at zero or more processed children here
		auto state = static_cast<dom_builder_state*>(opaque);
		vector<IDomObject^> expanded;
		consume_text(text, state, expanded);
		auto newDomId = state->domId++;
		auto result = ref new Code(expanded, newDomId);
		result->IsBlock = false;
		state->unclaimedDomIdMap[newDomId] = result;
		makeDomId(ob, newDomId, opaque);
		return 1;
	}

	static int rndr_triple_emphasis(struct buf *ob, const struct buf *text, void *opaque)
	{
		//we should be looking at zero or more processed children here
		auto state = static_cast<dom_builder_state*>(opaque);
		vector<uint32_t> splatIds;
		splat_text(text, state, splatIds);
		for(auto id : splatIds)
		{
			((Text^)state->unclaimedDomIdMap[id])->Bold = true;
			makeDomId(ob, id, opaque);
		}
		
		return 1;
	}

	static int rndr_double_emphasis(struct buf *ob, const struct buf *text, void *opaque) 
	{
		//we should be looking at zero or more processed children here
		auto state = static_cast<dom_builder_state*>(opaque);
		vector<uint32_t> splatIds;
		splat_text(text, state, splatIds);
		for(auto id : splatIds)
		{
			auto textElement = dynamic_cast<Text^>(state->unclaimedDomIdMap[id]);
			if(textElement != nullptr)
				textElement->Bold = true;
			makeDomId(ob, id, opaque);
		}
		return 1;
	}

	static int rndr_emphasis(struct buf *ob, const struct buf *text, void *opaque) 
	{
		//we should be looking at zero or more processed children here
		auto state = static_cast<dom_builder_state*>(opaque);
		vector<uint32_t> splatIds;
		splat_text(text, state, splatIds);
		for(auto id : splatIds)
		{
			auto textElement = dynamic_cast<Text^>(state->unclaimedDomIdMap[id]);
			if(textElement != nullptr)
				textElement->Italic = true;
			
			makeDomId(ob, id, opaque);
		}
		return 1;
	}

	static int rndr_strikethrough(struct buf *ob, const struct buf *text, void *opaque) 
	{
		//we should be looking at zero or more processed children here
		auto state = static_cast<dom_builder_state*>(opaque);
		vector<uint32_t> splatIds;
		splat_text(text, state, splatIds);
		for(auto id : splatIds)
		{
			auto textElement = dynamic_cast<Text^>(state->unclaimedDomIdMap[id]);
			if(textElement != nullptr)
				textElement->Strike = true;
			makeDomId(ob, id, opaque);
		}
		return 1;
	}

	static int rndr_superscript(struct buf *ob, const struct buf *text, void *opaque)
	{
		//we should be looking at zero or more processed children here
		auto state = static_cast<dom_builder_state*>(opaque);
		vector<uint32_t> splatIds;
		splat_text(text, state, splatIds);
		for(auto id : splatIds)
		{
			auto textElement = dynamic_cast<Text^>(state->unclaimedDomIdMap[id]);
			if(textElement != nullptr)
				textElement->Superscript = true;
			makeDomId(ob, id, opaque);
		}
		return 1;
	}

	static void rndr_header(struct buf *ob, const struct buf *text, int level, void *opaque)
	{
		if(level > 6) level = 4;
		if(level < 1) level = 4;

		//we should be looking at zero or more processed children here
		auto state = static_cast<dom_builder_state*>(opaque);
		vector<uint32_t> splatIds;
		splat_text(text, state, splatIds);
		for(auto id : splatIds)
		{
			auto textElement = dynamic_cast<Text^>(state->unclaimedDomIdMap[id]);
			if(textElement != nullptr)
				textElement->HeaderSize = level;
			makeDomId(ob, id, opaque);
		}
	}

	static int rndr_link(struct buf *ob, const struct buf *link, const struct buf *title, const struct buf *content, void *opaque) 
	{
		//we should be looking at zero or more processed children here
		auto state = static_cast<dom_builder_state*>(opaque);
		vector<IDomObject^> expandedObjects;
		if(content != nullptr && content->size > 0)
			consume_text(content, state, expandedObjects);

		auto newDomId = state->domId++;
		auto linkUrl = toPlatformString(link);
		IDomObject^ result = nullptr;
		result = ref new Link(linkUrl,
			toPlatformString(title),expandedObjects, newDomId);
		state->unclaimedDomIdMap[newDomId] = result;
		makeDomId(ob, newDomId, opaque);
		return 1;  
	}

	static void rndr_list(struct buf *ob, const struct buf *text, int flags, void *opaque)
	{
		//we should be looking at zero or more processed children here
		auto state = static_cast<dom_builder_state*>(opaque);
		vector<IDomObject^> expanded;
		consume_text(text, state, expanded);
		IDomObject^ result;
		auto newDomId = state->domId++;
		if(flags == MKD_LIST_ORDERED)
			result = ref new OrderedList(expanded, newDomId);
		else
			result = ref new UnorderedList(expanded, newDomId);

		state->unclaimedDomIdMap[newDomId] = result;
		makeDomId(ob, newDomId, opaque);
	}

	static void rndr_listitem(struct buf *ob, const struct buf *text, int flags, void *opaque)
	{
		//we should be looking at zero or more processed children here
		auto state = static_cast<dom_builder_state*>(opaque);
		vector<IDomObject^> expanded;
		consume_text(text, state, expanded);
		auto newDomId = state->domId++;
		auto result = ref new Paragraph(expanded, newDomId);
		state->unclaimedDomIdMap[newDomId] = result;
		makeDomId(ob, newDomId, opaque);
	}

	static void rndr_paragraph(struct buf *ob, const struct buf *text, void *opaque) 
	{
		//we should be looking at zero or more processed children here
		auto state = static_cast<dom_builder_state*>(opaque);
		vector<IDomObject^> expanded;
		consume_text(text, state, expanded);
		auto newDomId = state->domId++;
		auto result = ref new Paragraph(expanded, newDomId);
		state->unclaimedDomIdMap[newDomId] = result;
		makeDomId(ob, newDomId, opaque);
	}

	static void rndr_hrule(struct buf *ob, void *opaque) 
	{
		//we should be looking at zero or more processed children here
		auto state = static_cast<dom_builder_state*>(opaque);
		auto newDomId = state->domId++;
		auto result = ref new HorizontalRule(newDomId);
		state->unclaimedDomIdMap[newDomId] = result;
		makeDomId(ob, newDomId, opaque);
	}

	static int rndr_linebreak(struct buf *ob, void *opaque) 
	{
		//we should be looking at zero or more processed children here
		auto state = static_cast<dom_builder_state*>(opaque);
		auto newDomId = state->domId++;
		auto result = ref new LineBreak(newDomId);
		state->unclaimedDomIdMap[newDomId] = result;
		makeDomId(ob, newDomId, opaque);
		
		return 1; 
	}

	void rndr_table(struct buf *ob, const struct buf *header, const struct buf *body, void *opaque)
	{
		//we should be looking at zero or more processed children here
		auto state = static_cast<dom_builder_state*>(opaque);
		vector<IDomObject^> expanded;
		if(body != nullptr && body->size != 0)
			consume_text(body, state, expanded);

		vector<IDomObject^> expandedHeader;
		if(header != nullptr && header->size != 0)
			consume_text(header, state, expandedHeader);
		auto newDomId = state->domId++;
		auto result = ref new Table(expandedHeader, expanded, newDomId);
		state->unclaimedDomIdMap[newDomId] = result;
		makeDomId(ob, newDomId, opaque);
	}

	void rndr_table_row(struct buf *ob, const struct buf *text, void *opaque)
	{
		//we should be looking at zero or more processed children here
		auto state = static_cast<dom_builder_state*>(opaque);
		vector<IDomObject^> expanded;
		consume_text(text, state, expanded);
		auto newDomId = state->domId++;
		auto result = ref new TableRow(expanded, newDomId);
		state->unclaimedDomIdMap[newDomId] = result;
		makeDomId(ob, newDomId, opaque);
	}

	void rndr_table_cell(struct buf *ob, const struct buf *text, int flags, void *opaque)
	{
		//we should be looking at zero or more processed children here
		auto state = static_cast<dom_builder_state*>(opaque);
		vector<IDomObject^> expanded;
		consume_text(text, state, expanded);
		auto newDomId = state->domId++;
		auto result = ref new TableColumn(expanded, newDomId, flags);
		state->unclaimedDomIdMap[newDomId] = result;
		makeDomId(ob, newDomId, opaque);
	}

	void* rndr_allocate(void *opaque, size_t size)
	{
		auto state = static_cast<dom_builder_state*>(opaque);
		return state->memoryPool.alloc(size);
	}

	/* exported renderer structure */
	const struct sd_callbacks mkd_dom = 
	{
		rndr_blockcode,
		rndr_blockquote,
		NULL,
		rndr_header,
		rndr_hrule,
		rndr_list,
		rndr_listitem,
		rndr_paragraph,
		rndr_table,
		rndr_table_row,
		rndr_table_cell,

		rndr_autolink,
		rndr_codespan,
		rndr_double_emphasis,
		rndr_emphasis,
		NULL,
		rndr_linebreak,
		rndr_link,
		NULL,
		rndr_triple_emphasis,
		rndr_strikethrough,
		rndr_superscript,

		NULL,
		NULL,

		NULL,
		NULL,
		rndr_allocate
	};

	static void toBufString(const wchar_t* src, uint32_t srcLength, buf* target, void* opaque, void* (*allocate)(void *opaque, size_t size))
	{
		if(src == nullptr)
		{
			return;
		}
		int length = srcLength * 2;
		bufgrow(opaque, allocate, target, length);
		length = WideCharToMultiByte(CP_UTF8, 0, src, srcLength, (char*)target->data, length, NULL, NULL);
		if(length == -1)
			target->size = 0;
		else
			target->size = length;
	}

	static const unsigned int snudown_default_md_flags =
		MKDEXT_NO_INTRA_EMPHASIS |
		MKDEXT_SUPERSCRIPT |
		MKDEXT_AUTOLINK |
		MKDEXT_STRIKETHROUGH |
		MKDEXT_TABLES;

	public ref class SnuDom sealed
	{
	public:
		static Document^ MarkdownToDOM(Platform::String^ source)
		{
			try
			{
				//when this goes out of scope, all memory consumed by this session will be freed
				dom_builder_state processorState;
				buf* g_ib = bufnew(&processorState,mkd_dom.allocate, 1024);
				buf* g_ob = bufnew(&processorState,mkd_dom.allocate, 1024);
				toBufString(source->Data(), source->Length(), g_ib, &processorState,mkd_dom.allocate);

				
				auto markdownProcessor = sd_markdown_new(snudown_default_md_flags, 100, &mkd_dom, &processorState);

				sd_markdown_render(g_ob, g_ib->data, g_ib->size, markdownProcessor);
				vector<IDomObject^> topLevelObjects;
				consume_text(g_ob, &processorState, topLevelObjects);
				return ref new Document(topLevelObjects);
			}
			catch(...)
			{

			}
		return nullptr;
		}
	};

	public ref class SnuDomPlainTextVisitor sealed : IDomVisitor
	{
	public:
		property Platform::String^ Result;
		SnuDomPlainTextVisitor()
		{
			Result = "";
		}

		virtual void Visit(Text^ text) 
		{ 
			Result += text->Contents; 
		}
		virtual void Visit(Code^ code) { }
		virtual void Visit(Quote^ quote) { }
		virtual void Visit(OrderedList^ orderedList) { }
		virtual void Visit(UnorderedList^ unorderedList) { }
		virtual void Visit(HorizontalRule^ horizontalRule) { }
		virtual void Visit(Table^ table) { }
		virtual void Visit(Link^ link) { }
		virtual void Visit(Paragraph^ paragraph)
		{
			for (auto elem : paragraph)
            {
                elem->Accept(this);
            }
		}
		virtual void Visit(Document^ document)
		{
			for (auto elem : document)
            {
                elem->Accept(this);
            }
		}
		virtual void Visit(TableRow^ tableRow) {}
		virtual void Visit(TableColumn^ tableColumn) {}
		virtual void Visit(LineBreak^ lineBreak) { }
	};

	public enum class MarkdownCategory
	{
		PlainText,
		Formatted,
		Full
	};

	public ref class SnuDomCategoryVisitor sealed : IDomVisitor
	{
	public:
		
	private:
		void UpgradeCategory(MarkdownCategory category)
        {
            if ((int)Category < (int)category)
                Category = category;
        }

	public:
		SnuDomCategoryVisitor()
		{
			Category = MarkdownCategory::PlainText;
		}

		property MarkdownCategory Category;

		virtual void Visit(Text^ text)
		{
			if (text->Bold || text->Italic || text->HeaderSize != 0)
                UpgradeCategory(MarkdownCategory::Formatted);

            if(text->Strike || text->Superscript)
                UpgradeCategory(MarkdownCategory::Full);
		}
		virtual void Visit(Code^ code)
		{
			Category = MarkdownCategory::Full;
		}
		virtual void Visit(Quote^ quote)
		{
			Category = MarkdownCategory::Full;
		}
		virtual void Visit(OrderedList^ orderedList)
		{
			Category = MarkdownCategory::Full;
		}
		virtual void Visit(UnorderedList^ unorderedList)
		{
			Category = MarkdownCategory::Full;
		}
		virtual void Visit(HorizontalRule^ horizontalRule)
		{
			Category = MarkdownCategory::Full;
		}
		virtual void Visit(Table^ table)
		{
			Category = MarkdownCategory::Full;
		}
		virtual void Visit(Link^ link)
		{
			Category = MarkdownCategory::Full;
		}
		virtual void Visit(Paragraph^ paragraph)
		{
			int count = 0;
			for (auto elem : paragraph)
            {
				if(count == 1)
				{
					UpgradeCategory(MarkdownCategory::Formatted);
				}
				count++;
                elem->Accept(this);
            }
		}
		virtual void Visit(Document^ document)
		{
			int count = 0;
			for (auto elem : document)
            {
				if(count == 1)
				{
					UpgradeCategory(MarkdownCategory::Formatted);
				}
				count++;
                elem->Accept(this);
				if(Category == MarkdownCategory::Full)
					break;
            }
		}
		virtual void Visit(TableRow^ tableRow)
		{
			for (auto elem : tableRow->Columns)
            {
                elem->Accept(this);
            }
		}
		virtual void Visit(TableColumn^ tableColumn)
		{
			for (auto elem : tableColumn->Contents)
            {
                elem->Accept(this);
            }
		}
		virtual void Visit(LineBreak^ lineBreak)
		{
			UpgradeCategory(MarkdownCategory::Full);
		}
	};
	
}