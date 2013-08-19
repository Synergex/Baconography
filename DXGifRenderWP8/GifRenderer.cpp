#include "GifRenderer.h"

using namespace DXGifRenderWP8;
using namespace Platform;

Microsoft::WRL::ComPtr<ID3D11Device>                g_d3dDevice;
Microsoft::WRL::ComPtr<ID2D1Device>                 g_d2dDevice;
bool GifRenderer::_suspended = false;


struct gif_user_data
{
  int position;
  int length;
  uint8_t* data;
};

int istreamReader(GifFileType * gft, GifByteType * buf, int length)
{
  auto egi = reinterpret_cast<gif_user_data*>(gft->UserData);
  if (egi->position == egi->length) return 0;
  if (egi->position + length == egi->length) length = egi->length - egi->position;
  memcpy(buf, egi->data + egi->position, length);
  egi->position += length;

  return length;
}

GifRenderer^ CreateGifRenderer(const Platform::Array<std::uint8_t>^ asset)
{
  int error = 0;
  gif_user_data userData = { 0, asset->Length, asset->Data };
  GifFileType* gifFile = DGifOpen(&userData, istreamReader, &error);

  D2D1::ColorF backgroundColor(0);
  UINT width;
  UINT height; 
  int loopCount; 
  bool hasLoop;

  return ref new GifRenderer(gifFile, backgroundColor, width, height, loopCount, hasLoop);
}

GifRenderer::GifRenderer(GifFileType* gifFile, D2D1::ColorF& backgroundColor, UINT width, UINT height, int loopCount, bool hasLoop) : 
  Windows::UI::Xaml::Media::Imaging::SurfaceImageSource(width, height)
{
}
