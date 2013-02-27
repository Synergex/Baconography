#include "SoldOut.h"
#include "markdown.h"
#include <cstdint>
#include <string>
#include <algorithm>
#include <functional>
#include <cctype>
#include <ppl.h>

#ifndef WP8
using namespace SoldOutW8;
#else
using namespace SoldOutWP8;
#endif
using namespace Platform;
using namespace Concurrency;

bool has_ending (std::string const &fullString, std::string const &ending)
{
    if (fullString.length() >= ending.length()) {
        return (0 == fullString.compare (fullString.length() - ending.length(), ending.length(), ending));
    } else {
        return false;
    }
}

struct url {
    url(const std::string& url_s); // omitted copy, ==, accessors, ...
public:
    std::string protocol_, host_, path_, query_;
};

url::url(const std::string& url_s)
{
	using std::string;
	using std::transform;
	using std::ptr_fun;

    const string prot_end("://");
    string::const_iterator prot_i = search(url_s.begin(), url_s.end(),
                                           prot_end.begin(), prot_end.end());
    protocol_.reserve(distance(url_s.begin(), prot_i));
    transform(url_s.begin(), prot_i,
              back_inserter(protocol_),
              ptr_fun<int,int>(tolower)); // protocol is icase
    if( prot_i == url_s.end() )
        return;
    advance(prot_i, prot_end.length());
    string::const_iterator path_i = find(prot_i, url_s.end(), '/');
    host_.reserve(distance(prot_i, path_i));
    transform(prot_i, path_i,
              back_inserter(host_),
              ptr_fun<int,int>(tolower)); // host is icase
    string::const_iterator query_i = find(path_i, url_s.end(), '?');
    path_.assign(path_i, query_i);
    if( query_i != url_s.end() )
        ++query_i;
    query_.assign(query_i, url_s.end());
}

bool is_url_known_image(uint8_t* ptr, size_t length)
{
	std::string str((char*)ptr, length);
	if(has_ending(str, ".gif"))
		return false;
	else if(has_ending(str, ".jpg") || has_ending(str, ".png"))
		return true;
	else
	{
		std::transform(str.begin(), str.end(), str.begin(), ::tolower);
		url parsedUrl(str);
		std::string& host = parsedUrl.host_; 
		if(host == "imgur.com" ||
			host == "min.us" ||
			host == "www.quickmeme.com" ||
			host == "i.qkme.me" ||
			host == "quickmeme.com" ||
			host == "qkme.me" ||
			host == "memecrunch.com" ||
			host == "flickr.com")
		{
			return true;
		}
		else
			return false;
		
	}
}

/*****************************
 * EXPORTED HELPER FUNCTIONS *
 *****************************/

/* lus_attr_escape • copy the buffer entity-escaping '<', '>', '&' and '"' */
void
lus_attr_escape(struct buf *ob, uint8_t *src, size_t size) {
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
lus_body_escape(struct buf *ob, uint8_t *src, size_t size) {
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
rndr_autolink(struct buf *ob, const struct buf *link, enum mkd_autolink type, void *opaque) {

	if (!link || !link->size) return 0;
#ifndef WP8
	if(is_url_known_image(link->data, link->size))
	{
		if (!link || !link->size) return 0;
		BUFPUTSL(ob, "<InlineUIContainer><Grid><Grid.ColumnDefinitions><ColumnDefinition Width=\"Auto\"/><ColumnDefinition Width=\"*\"/></Grid.ColumnDefinitions><Button VerticalAlignment=\"Top\" Grid.Column=\"0\" Command=\"{Binding Path=StaticCommands.GotoMarkdownLink, Mode=OneTime}\" Style=\"{Binding TextButtonStyle, Mode=OneTime}\" Margin=\"0,0,0,0\" Padding=\"0\" CommandParameter=\"");
		lus_attr_escape(ob, link->data, link->size);
		BUFPUTSL(ob, "\"><Button.Foreground><Binding Converter=\"{Binding VisitedLink, Source={StaticResource Locator}}\" ConverterParameter=\"");
		lus_attr_escape(ob, link->data, link->size);
		BUFPUTSL(ob, "\"/></Button.Foreground><Button.Content>");
		lus_attr_escape(ob, link->data, link->size);
		BUFPUTSL(ob, "</Button.Content></Button><view:ImagePreviewWithButtonView Grid.Column=\"1\" DataContext=\"");
		lus_attr_escape(ob, link->data, link->size);
		BUFPUTSL(ob, "\"/></Grid></InlineUIContainer>");
	}
	else
	{
		BUFPUTSL(ob, "<InlineUIContainer><Button Command=\"{Binding Path=StaticCommands.GotoMarkdownLink, Mode=OneTime}\" Style=\"{Binding TextButtonStyle, Mode=OneTime}\" Margin=\"0,0,0,0\" Padding=\"0\" CommandParameter=\"");
		lus_attr_escape(ob, link->data, link->size);
		BUFPUTSL(ob, "\"><Button.Foreground><Binding Converter=\"{Binding VisitedLink, Source={StaticResource Locator}}\" ConverterParameter=\"");
		lus_attr_escape(ob, link->data, link->size);
		BUFPUTSL(ob, "\"/></Button.Foreground><Button.Content>");
		lus_attr_escape(ob, link->data, link->size);
		BUFPUTSL(ob, "</Button.Content></Button></InlineUIContainer>");
	}
#else
		BUFPUTSL(ob, "<InlineUIContainer><common:MarkdownButton Url=\"");
		lus_attr_escape(ob, link->data, link->size);
		BUFPUTSL(ob, "\"/></InlineUIContainer>");
#endif

	return 1; 
}

static void
rndr_blockcode(struct buf *ob, const struct buf *text, const struct buf *lang, void *opaque) {
	if (ob->size) bufputc(ob, '\n');
	BUFPUTSL(ob, "\n");
	if (text) lus_body_escape(ob, text->data, text->size);
	BUFPUTSL(ob, "\n"); 
}

static void
rndr_blockquote(struct buf *ob, const struct buf *text, void *opaque) {
	if (ob->size) bufputc(ob, '\n');
	BUFPUTSL(ob, "\n");
	if (text) lus_body_escape(ob, text->data, text->size);
	BUFPUTSL(ob, "\n");
}

static int
rndr_codespan(struct buf *ob,const  struct buf *text, void *opaque) {
	if (ob->size) bufputc(ob, '\n');
	BUFPUTSL(ob, "\n");
	if (text) lus_body_escape(ob, text->data, text->size);
	BUFPUTSL(ob, "\n");
	return 1; 
}

static int
rndr_double_emphasis(struct buf *ob, const struct buf *text, void *opaque) {
	if (!text || !text->size) return 0;
	BUFPUTSL(ob, "<Span FontWeight=\"Bold\">");
	bufput(ob, text->data, text->size);
	BUFPUTSL(ob, "</Span>");
	return 1; 
}

static int
rndr_emphasis(struct buf *ob, const struct buf *text, void *opaque) {
	if (!text || !text->size) return 0;
	BUFPUTSL(ob, "<Span FontStyle=\"Italic\">");
	if (text) bufput(ob, text->data, text->size);
	BUFPUTSL(ob, "</Span>");
	return 1; }

static int
rndr_strikethrough(struct buf *ob, const struct buf *text, void *opaque) {
	if (!text || !text->size) return 0;
	//BUFPUTSL(ob, "<Span FontStyle=\"Italic\">");
	if (text) bufput(ob, text->data, text->size);
	//BUFPUTSL(ob, "</Span>");
	return 1; }

static int
rndr_superscript(struct buf *ob, const struct buf *text, void *opaque) {
	if (!text || !text->size) return 0;
	//BUFPUTSL(ob, "<Span FontStyle=\"Italic\">");
	if (text) bufput(ob, text->data, text->size);
	//BUFPUTSL(ob, "</Span>");
	return 1; }

static void
rndr_header(struct buf *ob, const struct buf *text, int level, void *opaque) {
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
rndr_link(struct buf *ob, const struct buf *link, const struct buf *title, const struct buf *content, void *opaque) 
{

#ifndef WP8
	if(is_url_known_image(link->data, link->size))
	{
		if (!link || !link->size) return 0;
		BUFPUTSL(ob, "<InlineUIContainer><Grid><Grid.ColumnDefinitions><ColumnDefinition Width=\"Auto\"/><ColumnDefinition Width=\"*\"/></Grid.ColumnDefinitions><Button VerticalAlignment=\"Top\" Grid.Column=\"0\" Command=\"{Binding Path=StaticCommands.GotoMarkdownLink, Mode=OneTime}\" Style=\"{Binding TextButtonStyle, Mode=OneTime}\" Margin=\"0,0,0,0\" Padding=\"0\" CommandParameter=\"");
		lus_attr_escape(ob, link->data, link->size);
		BUFPUTSL(ob, "\"><Button.Foreground><Binding Converter=\"{Binding VisitedLink, Source={StaticResource Locator}}\" ConverterParameter=\"");
		lus_attr_escape(ob, link->data, link->size);
		BUFPUTSL(ob, "\"/></Button.Foreground><Button.Content>");
		if (content && content->size) bufput(ob, content->data, content->size);
		BUFPUTSL(ob, "</Button.Content></Button><view:ImagePreviewWithButtonView Grid.Column=\"1\" DataContext=\"");
		lus_attr_escape(ob, link->data, link->size);
		BUFPUTSL(ob, "\"/></Grid></InlineUIContainer>");
	}
	else
	{
		if (!link || !link->size) return 0;
		BUFPUTSL(ob, "<InlineUIContainer><Button Command=\"{Binding Path=StaticCommands.GotoMarkdownLink, Mode=OneTime}\" Style=\"{Binding TextButtonStyle, Mode=OneTime}\" Margin=\"0,0,0,0\" Padding=\"0\" CommandParameter=\"");
		lus_attr_escape(ob, link->data, link->size);
		BUFPUTSL(ob, "\"><Button.Foreground><Binding Converter=\"{Binding VisitedLink, Source={StaticResource Locator}}\" ConverterParameter=\"");
		lus_attr_escape(ob, link->data, link->size);
		BUFPUTSL(ob, "\"/></Button.Foreground><Button.Content>");
		if (content && content->size) bufput(ob, content->data, content->size);
		BUFPUTSL(ob, "</Button.Content></Button></InlineUIContainer>");
	}
#else
		BUFPUTSL(ob, "<InlineUIContainer><common:MarkdownButton Url=\"");
		lus_attr_escape(ob, link->data, link->size);
		BUFPUTSL(ob, "\"/></InlineUIContainer>");
#endif
	return 1;  
}

static void
rndr_list(struct buf *ob, const struct buf *text, int flags, void *opaque) {
	if (ob->size) bufputc(ob, '\n');
	if (text) bufput(ob, text->data, text->size);
}

static void
rndr_listitem(struct buf *ob, const struct buf *text, int flags, void *opaque) {
	BUFPUTSL(ob, "•  ");
	if (text) {
		bufput(ob, text->data, text->size); }
	BUFPUTSL(ob, "<LineBreak/>\n"); 
}

static void
rndr_normal_text(struct buf *ob, const struct buf *text, void *opaque) {
	if (text) lus_body_escape(ob, text->data, text->size); 
}

static void
rndr_paragraph(struct buf *ob, const struct buf *text, void *opaque) {
	if (ob->size) bufputc(ob, '\n');
	BUFPUTSL(ob, "<Paragraph>\n");
	if (text) bufput(ob, text->data, text->size);
	BUFPUTSL(ob, "</Paragraph>\n"); 
}

static void
rndr_raw_block(struct buf *ob,const  struct buf *text, void *opaque) {
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
rndr_raw_inline(struct buf *ob, const struct buf *tag, void *opaque) {
	bufput(ob, tag->data, tag->size);
	return 1;
}

static int
rndr_triple_emphasis(struct buf *ob, const struct buf *text, void *opaque) {
	if (!text || !text->size) return 0;
	BUFPUTSL(ob, "<Span FontWeight=\"Bold\">");
	bufput(ob, text->data, text->size);
	BUFPUTSL(ob, "</Span>");
	return 1; 
}

static void
rndr_hrule(struct buf *ob, void *opaque) {
	if (ob->size) bufputc(ob, '\n');
	BUFPUTSL(ob, "<Paragraph><InlineUIContainer><Line HorizontalAlignment=\"Stretch\" Stretch=\"Fill\"/></InlineUIContainer></Paragraph>\n"); 
}

static int
rndr_linebreak(struct buf *ob, void *opaque) {
	BUFPUTSL(ob, "<LineBreak />\n");
	return 1; 
}

/* exported renderer structure */
const struct sd_callbacks mkd_xaml = {
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
	rndr_strikethrough,
	rndr_superscript,

	NULL,
	rndr_normal_text,

	NULL,
	NULL};

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
	length = wcstombs((char*)target->data, src->Data(), length) ;
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

critical_section markdownCriticalSection;
sd_markdown* markdownProcessor = nullptr;

Platform::String^ SoldOut::MarkdownToXaml(Platform::String^ source)
{
	try
	{
		critical_section::scoped_lock markdownLock(markdownCriticalSection);
		auto ib = bufnew(1024);
		auto ob = bufnew(64);

		toBufString(source, ib);

		if(markdownProcessor == nullptr)
			markdownProcessor = sd_markdown_new(snudown_default_md_flags, 100, &mkd_xaml, NULL);

		sd_markdown_render(ob, ib->data, ib->size, markdownProcessor);

		//sd_markdown_free(markdownProc);

		auto result = toPlatformString((char*)ob->data, ob->size);

		bufrelease(ib);
		bufrelease(ob);
		return result;
	}
	catch(...)
	{

	}
	return nullptr;
}
