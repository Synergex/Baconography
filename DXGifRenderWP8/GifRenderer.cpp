#include "GifRenderer.h"
#include "SpriteBatch.h"

using namespace DXGifRenderWP8;
using namespace Platform;
using namespace DirectX;

using std::uint8_t;
using Microsoft::WRL::ComPtr;
using Windows::Foundation::Rect;
using namespace Microsoft::WRL;
using namespace Windows::Phone::Graphics::Interop;

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


void mapRasterBits(uint8_t* rasterBits, std::unique_ptr<uint32_t>& targetFrame, ColorMapObject * colorMap, int top, int left, int bottom, int right, int width, uint8_t transparencyColor)
{
	struct bgraColor
	{
		uint8_t blue;
		uint8_t green;
		uint8_t red;
		uint8_t alpha;
	};
	int i = 0;
	for (int y = top; y < bottom; y++)
    {
		for (int x = left; x < right; x++)
		{
			int offset = y * width + x;
			int index = rasterBits[i];

			if (transparencyColor == -1||
				transparencyColor != index)
			{
				auto& gifColor = colorMap->Colors[index];
				bgraColor color = { gifColor.Blue, gifColor.Green, gifColor.Red, (uint8_t)255};
				targetFrame.get()[offset ] = *((uint32_t*)&color);
			}
			i++;
		}
	}
}

void loadTexture(ID3D11Device1* d3dDevice, ID3D11DeviceContext1* context, GifFrame& frame, std::unique_ptr<uint32_t>& buffer)
{
	CD3D11_TEXTURE2D_DESC textureDesc(
		DXGI_FORMAT_B8G8R8A8_UNORM, 
		frame.width, //picture width
		frame.height,
		1, 1);
	
	D3D11_SUBRESOURCE_DATA data;
	data.pSysMem = buffer.get();
	data.SysMemPitch = 4 * frame.width;
	data.SysMemSlicePitch = 0;
	DXGifRenderWP8::ThrowIfFailed(d3dDevice->CreateTexture2D(&textureDesc, &data, &frame.preRendered));

	D3D11_SHADER_RESOURCE_VIEW_DESC SRVDesc;
    memset( &SRVDesc, 0, sizeof( SRVDesc ) );
    SRVDesc.Format = DXGI_FORMAT_B8G8R8A8_UNORM;
    SRVDesc.ViewDimension = D3D11_SRV_DIMENSION_TEXTURE2D;
    SRVDesc.Texture2D.MipLevels = 1;

	DXGifRenderWP8::ThrowIfFailed(d3dDevice->CreateShaderResourceView(frame.preRendered.Get(), &SRVDesc, &frame.resource));
}


void loadGifFrames(GifFileType* gifFile, std::vector<GifFrame>& frames, ID3D11Device1* d3dDevice, ID3D11DeviceContext1* context)
{
	UINT width = gifFile->SWidth;
	UINT height = gifFile->SHeight; 
	int loopCount = 0; 
	bool hasLoop = true;

	int bgRed = 0;
	int bgGreen = 0;
	int bgBlue = 0;

	if(gifFile->SColorMap != nullptr)
	{
		auto color = gifFile->SColorMap->Colors[gifFile->SBackGroundColor];
		bgRed = color.Red;
		bgGreen = color.Green;
		bgBlue = color.Blue;
	}


	std::unique_ptr<uint32_t> buffer = std::unique_ptr<uint32_t>(new uint32_t[width * height]);
	std::unique_ptr<uint32_t> lastFrame = std::unique_ptr<uint32_t>(new uint32_t[width * height]);

	uint8_t* bufPtr = (uint8_t*)buffer.get();
	uint8_t* lastFramePtr = (uint8_t*)lastFrame.get();

	for (int y = 0; y < height; y++)
	{
		for (int x = 0; x < width; x++)
		{
			int offset = y * width + x;
			

			lastFramePtr[offset * 4 + 0] = bufPtr[offset * 4 + 0] = bgBlue;
			lastFramePtr[offset * 4 + 1] = bufPtr[offset * 4 + 1] = bgGreen;
			lastFramePtr[offset * 4 + 2] = bufPtr[offset * 4 + 2] = bgRed;
			lastFramePtr[offset * 4 + 3] = bufPtr[offset * 4 + 3] = 255;
		}
	}

	for(int i = 0; i < gifFile->ImageCount; i++)
	{
		uint32_t delay ;
		DISPOSAL_METHODS disposal;
		int8_t transparentColor = -1;

		auto extensionBlocks = gifFile->SavedImages[i].ExtensionBlocks;
		for(int ext = 0; ext < gifFile->SavedImages[i].ExtensionBlockCount; ext++)
		{
			if(extensionBlocks[ext].Function == 0xF9)
			{
				GraphicsControlBlock gcb;
				DGifExtensionToGCB(extensionBlocks[ext].ByteCount, extensionBlocks[ext].Bytes, &gcb);
				
				UIntMult(gcb.DelayTime, 10, &delay);

				if (delay < 20)
				{
					delay = 100;
				}

				disposal = (DISPOSAL_METHODS)gcb.DisposalMode;
				transparentColor = gcb.TransparentColor;
			}
		}

		auto& imageDesc = gifFile->SavedImages[i].ImageDesc;
		int right =  imageDesc.Left + imageDesc.Width;
		int bottom = imageDesc.Top + imageDesc.Height;
		int top = imageDesc.Top;
		int left = imageDesc.Left;
		SavedImage* decodeFrame = gifFile->SavedImages + i;

		frames.push_back(GifFrame());
		auto& frame = frames.back();
		frame.height = height;
		frame.width = width;
		frame.delay = delay;
		auto colorMap = (decodeFrame->ImageDesc.ColorMap != nullptr ? decodeFrame->ImageDesc.ColorMap : (gifFile->SColorMap != nullptr ? gifFile->SColorMap : nullptr));

		if(disposal == DISPOSAL_METHODS::DM_PREVIOUS)
		{
			memcpy(lastFrame.get(), buffer.get(), width * height * sizeof(uint32_t));
		}

		mapRasterBits(decodeFrame->RasterBits, buffer, colorMap, top, left, bottom, right, width, transparentColor);
		
		loadTexture(d3dDevice, context, frame, buffer);

		switch(disposal)
		{
		case DISPOSAL_METHODS::DM_BACKGROUND:
			for (int y = top; y < bottom; y++)
			{
				for (int x = left; x < right; x++)
				{
					int offset = y * width + x;

					bufPtr[offset * 4 + 0] = bgBlue;
					bufPtr[offset * 4 + 1] = bgGreen;
					bufPtr[offset * 4 + 2] = bgRed;
					bufPtr[offset * 4 + 3] = 255;
				}
			}
			break;
		case DISPOSAL_METHODS::DM_PREVIOUS:
			memcpy(buffer.get(), lastFrame.get(), width * height * sizeof(uint32_t));
			break;
		}

		
	}
}


GifRenderer::GifRenderer(GifFileType* gifFile)
{
	_gifFile = gifFile;
}


bool GifRenderer::Update(float total, float delta)
{
	double msDelta = ((double)total) * 1000; 
	double accountedFor = 0;
	int i = 0;
	for(; accountedFor < msDelta ;i++)
	{
		if(i >= _frames.size())
			i = 0;

		accountedFor += _frames[i].delay;
	}
	auto newFrame = i - 1;
	if(newFrame != _currentFrame || _currentFrame == 0)
	{
		_currentFrame = i - 1;
		return true;
	}
	else
		return false;
}

void GifRenderer::Render()
{
	if(_spriteBatch == nullptr)
		_spriteBatch = std::unique_ptr<SpriteBatch>(new DirectX::SpriteBatch(m_d3dContext.Get()));

	const float midnightBlue[] = { 0.098f, 0.098f, 0.439f, 1.000f };
	m_d3dContext->ClearRenderTargetView(
		m_renderTargetView.Get(),
		midnightBlue
		);

	m_d3dContext->ClearDepthStencilView(
		m_depthStencilView.Get(),
		D3D11_CLEAR_DEPTH,
		1.0f,
		0
		);

	m_d3dContext->OMSetRenderTargets(
		1,
		m_renderTargetView.GetAddressOf(),
		m_depthStencilView.Get()
		);

	_spriteBatch->Begin();
	auto& frame = _frames[_currentFrame];
	RECT rect = { 0, 0, frame.width, frame.height};
	_spriteBatch->Draw(frame.resource.Get(), rect, Colors::White);
	_spriteBatch->End();

	
}

void GifRenderer::CreateDeviceResources()
{
	Direct3DBase::CreateDeviceResources();
	try
	{
		if(_gifFile != nullptr)
		{
			loadGifFrames(_gifFile, _frames, m_d3dDevice.Get(), m_d3dContext.Get());
		}
	}
	catch(...)
	{
		throw ref new Platform::FailureException("error loading gif");
	}
}


