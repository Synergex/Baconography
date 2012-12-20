#include "SoldOut.h"
#include "markdown.h"
#include <cstdint>
#include <string>

using namespace SoldOutW8;
using namespace Platform;

/*****************************
 * EXPORTED HELPER FUNCTIONS *
 *****************************/

/* lus_attr_escape • copy the buffer entity-escaping '<', '>', '&' and '"' */
void
lus_attr_escape(struct buf *ob, char *src, size_t size) {
	size_t  i = 0, org;
	while (i < size) {
		/* copying directly unescaped characters */
		org = i;
		while (i < size && src[i] != '<' && src[i] != '>'
		&& src[i] != '&' && src[i] != '"')
			i += 1;
		if (i > org) bufput(ob, src + org, i - org);

		/* escaping */
		if (i >= size) break;
		else if (src[i] == '<') BUFPUTSL(ob, "&lt;");
		else if (src[i] == '>') BUFPUTSL(ob, "&gt;");
		else if (src[i] == '&') BUFPUTSL(ob, "&amp;");
		else if (src[i] == '"') BUFPUTSL(ob, "&quot;");
		i += 1; } 
}


/* lus_body_escape • copy the buffer entity-escaping '<', '>' and '&' */
void
lus_body_escape(struct buf *ob, char *src, size_t size) {
	size_t  i = 0, org;
	while (i < size) {
		/* copying directly unescaped characters */
		org = i;
		while (i < size && src[i] != '<' && src[i] != '>'
		&& src[i] != '&')
			i += 1;
		if (i > org) bufput(ob, src + org, i - org);

		/* escaping */
		if (i >= size) break;
		else if (src[i] == '<') BUFPUTSL(ob, "&lt;");
		else if (src[i] == '>') BUFPUTSL(ob, "&gt;");
		else if (src[i] == '&') BUFPUTSL(ob, "&amp;");
		i += 1; } 
}

static int
rndr_autolink(struct buf *ob, struct buf *link, enum mkd_autolink type,
						void *opaque) {
	if (!link || !link->size) return 0;
	BUFPUTSL(ob, "<InlineUIContainer><Button Command=\"{Binding Path=StaticCommands.GotoMarkdownLink, Mode=OneTime}\" Style=\"{Binding TextButtonStyle, Mode=OneTime}\" Margin=\"0,0,0,0\" Padding=\"0\" CommandParameter=\"");
	lus_attr_escape(ob, link->data, link->size);
	BUFPUTSL(ob, "\"><Button.Foreground><Binding Converter=\"{Binding VisitedLink, Source={StaticResource Locator}}\" ConverterParameter=\"");
	lus_attr_escape(ob, link->data, link->size);
	BUFPUTSL(ob, "\"/></Button.Foreground><Button.Content>");
	lus_attr_escape(ob, link->data, link->size);
	BUFPUTSL(ob, "</Button.Content></Button></InlineUIContainer>");
	return 1; 
}

static void
rndr_blockcode(struct buf *ob, struct buf *text, void *opaque) {
	if (ob->size) bufputc(ob, '\n');
	BUFPUTSL(ob, "\n");
	if (text) lus_body_escape(ob, text->data, text->size);
	BUFPUTSL(ob, "\n"); 
}

static void
rndr_blockquote(struct buf *ob, struct buf *text, void *opaque) {
	if (ob->size) bufputc(ob, '\n');
	BUFPUTSL(ob, "\n");
	if (text) lus_body_escape(ob, text->data, text->size);
	BUFPUTSL(ob, "\n");
}

static int
rndr_codespan(struct buf *ob, struct buf *text, void *opaque) {
	if (ob->size) bufputc(ob, '\n');
	BUFPUTSL(ob, "\n");
	if (text) lus_body_escape(ob, text->data, text->size);
	BUFPUTSL(ob, "\n");
	return 1; 
}

static int
rndr_double_emphasis(struct buf *ob, struct buf *text, char c, void *opaque) {
	if (!text || !text->size) return 0;
	BUFPUTSL(ob, "<Span FontWeight=\"Bold\">");
	bufput(ob, text->data, text->size);
	BUFPUTSL(ob, "</Span>");
	return 1; 
}

static int
rndr_emphasis(struct buf *ob, struct buf *text, char c, void *opaque) {
	if (!text || !text->size) return 0;
	BUFPUTSL(ob, "<Span FontStyle=\"Italic\">");
	if (text) bufput(ob, text->data, text->size);
	BUFPUTSL(ob, "</Span>");
	return 1; }

static void
rndr_header(struct buf *ob, struct buf *text, int level, void *opaque) {
	if (ob->size) bufputc(ob, '\n');

	BUFPUTSL(ob, "<Span FontSize=\"");

	switch (level)
	{
	case 1:
		BUFPUTSL(ob, "24");
		break;
	case 2:
		BUFPUTSL(ob, "20");
		break;
	case 3:
		BUFPUTSL(ob, "16");
		break;
	case 4:
		BUFPUTSL(ob, "12");
		break;
	case 5:
		BUFPUTSL(ob, "10");
		break;
	case 6:
		BUFPUTSL(ob, "8");
		break;
	default:
		BUFPUTSL(ob, "12");
		break;
	}

	BUFPUTSL(ob, "\">");
	if (text) bufput(ob, text->data, text->size);
	BUFPUTSL(ob, "</Span>"); 
}

static int
rndr_link(struct buf *ob, struct buf *link, struct buf *title,
			struct buf *content, void *opaque) 
{
	if (!link || !link->size) return 0;
	BUFPUTSL(ob, "<InlineUIContainer><Button Command=\"{Binding Path=StaticCommands.GotoMarkdownLink, Mode=OneTime}\" Style=\"{Binding TextButtonStyle, Mode=OneTime}\" Margin=\"0,0,0,0\" Padding=\"0\" CommandParameter=\"");
	lus_attr_escape(ob, link->data, link->size);
	BUFPUTSL(ob, "\"><Button.Foreground><Binding Converter=\"{Binding VisitedLink, Source={StaticResource Locator}}\" ConverterParameter=\"");
	lus_attr_escape(ob, link->data, link->size);
	BUFPUTSL(ob, "\"/></Button.Foreground><Button.Content>");
	if (content && content->size) bufput(ob, content->data, content->size);
	BUFPUTSL(ob, "</Button.Content></Button></InlineUIContainer>");
	return 1;  
}

static void
rndr_list(struct buf *ob, struct buf *text, int flags, void *opaque) {
	if (ob->size) bufputc(ob, '\n');
	BUFPUTSL(ob, "<Paragraph>\n");
	if (text) bufput(ob, text->data, text->size);
	BUFPUTSL(ob, "</Paragraph>");
}

static void
rndr_listitem(struct buf *ob, struct buf *text, int flags, void *opaque) {
	BUFPUTSL(ob, "•  ");
	if (text) {
		while (text->size && text->data[text->size - 1] == '\n')
			text->size -= 1;
		bufput(ob, text->data, text->size); }
	BUFPUTSL(ob, "<LineBreak/>\n"); 
}

static void
rndr_normal_text(struct buf *ob, struct buf *text, void *opaque) {
	if (text) lus_body_escape(ob, text->data, text->size); 
}

static void
rndr_paragraph(struct buf *ob, struct buf *text, void *opaque) {
	if (ob->size) bufputc(ob, '\n');
	BUFPUTSL(ob, "<Paragraph>\n");
	if (text) bufput(ob, text->data, text->size);
	BUFPUTSL(ob, "</Paragraph>\n"); 
}

static void
rndr_raw_block(struct buf *ob, struct buf *text, void *opaque) {
	size_t org, sz;
	if (!text) return;
	sz = text->size;
	while (sz > 0 && text->data[sz - 1] == '\n') sz -= 1;
	org = 0;
	while (org < sz && text->data[org] == '\n') org += 1;
	if (org >= sz) return;
	if (ob->size) bufputc(ob, '\n');
	bufput(ob, text->data + org, sz - org);
	bufputc(ob, '\n'); 
}

static int
rndr_raw_inline(struct buf *ob, struct buf *text, void *opaque) {
	bufput(ob, text->data, text->size);
	return 1;
}

static int
rndr_triple_emphasis(struct buf *ob, struct buf *text, char c, void *opaque) {
	if (!text || !text->size) return 0;
	BUFPUTSL(ob, "<Span FontStyle=\"Bold\">");
	bufput(ob, text->data, text->size);
	BUFPUTSL(ob, "</Span>");
	return 1; 
}

static void
rndr_hrule(struct buf *ob, void *opaque) {
	if (ob->size) bufputc(ob, '\n');
	BUFPUTSL(ob, "<InlineUIContainer><Line HorizontalAlignment=\"Stretch\" Stretch=\"Fill\"/></InlineUIContainer>\n"); 
}

static int
rndr_linebreak(struct buf *ob, void *opaque) {
	BUFPUTSL(ob, "<LineBreak />\n");
	return 1; 
}

/* exported renderer structure */
const struct mkd_renderer mkd_xaml = {
	NULL,
	NULL,

	rndr_blockcode,
	rndr_blockquote,
	rndr_raw_block,
	rndr_header,
	rndr_hrule,
	rndr_list,
	rndr_listitem,
	rndr_paragraph,
	NULL,
	NULL,
	NULL,

	rndr_autolink,
	rndr_codespan,
	rndr_double_emphasis,
	rndr_emphasis,
	NULL,
	rndr_linebreak,
	rndr_link,
	rndr_raw_inline,
	rndr_triple_emphasis,

	NULL,
	rndr_normal_text,

	64,
	"*_",
	NULL };

Platform::String^ toPlatformString(const char* src, uint32_t sourceLength)
{
	if (src == nullptr || sourceLength == 0)
		return nullptr;
			
	//get the length first so we dont have to double allocate
	//also its not actually possible to predetermine the size
	//as char16 just means the potential blocks are 16bits but remain chainable into a single char
	auto length = sourceLength;
	std::wstring result(length, 0);
	mbstowcs(&result[0], src, length);
	return ref new Platform::String(result.c_str(), (unsigned int)result.size());
}

static void toBufString(Platform::String^ src, buf* target)
{
	if(src == nullptr)
	{
		return;
	}
	int length = src->Length() * 2;
	bufgrow(target, length);
	length = wcstombs(target->data, src->Data(), length) ;
	if(length == -1)
		target->size = 0;
	else
		target->size = length;
}

Platform::String^ SoldOut::MarkdownToXaml(Platform::String^ source)
{
	try
	{
		auto ib = bufnew(1024);
		auto ob = bufnew(64);

		toBufString(source, ib);

		markdown(ob, ib, &mkd_xaml);

		auto result = toPlatformString(ob->data, ob->size);

		bufrelease(ib);
		bufrelease(ob);
		return result;
	}
	catch(...)
	{

	}
	return nullptr;
}
