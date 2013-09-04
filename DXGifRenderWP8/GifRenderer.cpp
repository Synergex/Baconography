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

struct bgraColor
{
	uint8_t blue;
	uint8_t green;
	uint8_t red;
	uint8_t alpha;
};

void mapRasterBits(uint8_t* rasterBits, std::unique_ptr<uint32_t>& targetFrame, ColorMapObject * colorMap, int top, int left, int bottom, int right, int width, int32_t transparencyColor)
{
	
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

void loadTexture(ID3D11Device1* d3dDevice, ID3D11DeviceContext1* context, std::unique_ptr<uint32_t>& buffer, int width, int height,
				 Microsoft::WRL::ComPtr<ID3D11Texture2D>& preRendered, Microsoft::WRL::ComPtr<ID3D11ShaderResourceView>& resource)
{
	CD3D11_TEXTURE2D_DESC textureDesc(
		DXGI_FORMAT_B8G8R8A8_UNORM, 
		width, //picture width
		height,
		1, 1);
	
	D3D11_SUBRESOURCE_DATA data;
	data.pSysMem = buffer.get();
	data.SysMemPitch = 4 * width;
	data.SysMemSlicePitch = 0;
	DXGifRenderWP8::ThrowIfFailed(d3dDevice->CreateTexture2D(&textureDesc, &data, &preRendered));

	D3D11_SHADER_RESOURCE_VIEW_DESC SRVDesc;
    memset( &SRVDesc, 0, sizeof( SRVDesc ) );
    SRVDesc.Format = DXGI_FORMAT_B8G8R8A8_UNORM;
    SRVDesc.ViewDimension = D3D11_SRV_DIMENSION_TEXTURE2D;
    SRVDesc.Texture2D.MipLevels = 1;

	DXGifRenderWP8::ThrowIfFailed(d3dDevice->CreateShaderResourceView(preRendered.Get(), &SRVDesc, &resource));
}

void loadGifFrames(GifFileType* gifFile, std::vector<GifFrame>& frames, ID3D11Device1* d3dDevice, ID3D11DeviceContext1* context)
{
	UINT width = gifFile->SWidth;
	UINT height = gifFile->SHeight; 
	int loopCount = 0; 
	bool hasLoop = true;

	for(int i = 0; i < gifFile->ImageCount; i++)
	{
		uint32_t delay ;
		DISPOSAL_METHODS disposal;
		int32_t transparentColor = -1;

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

		frames.push_back(GifFrame());
		auto& frame = frames.back();
		frame.transparentColor = transparentColor;
		frame.height = height;
		frame.width = width;
		frame.delay = delay;
		frame.top = top;
		frame.bottom = bottom;
		frame.right = right;
		frame.left = left;
		frame.imageData = gifFile->SavedImages + i;
		frame.disposal = disposal;
	}
}

void loadGifFrame(GifFileType* gifFile, GifFrame& frame, std::unique_ptr<uint32_t>& buffer, int currentFrame, int targetFrame)
{
	UINT width = gifFile->SWidth;
	UINT height = gifFile->SHeight; 
	int loopCount = 0; 
	bool hasLoop = true;

	bgraColor bgColor;
	if(gifFile->SColorMap != nullptr)
	{
		auto color = gifFile->SColorMap->Colors[gifFile->SBackGroundColor];
		bgColor.red = color.Red;
		bgColor.green = color.Green;
		bgColor.blue = color.Blue;
		bgColor.alpha = 255;
	}

	std::unique_ptr<uint32_t> lastFrame = nullptr;

	if(buffer == nullptr || targetFrame == 0 || currentFrame > targetFrame)
	{
		if(buffer == nullptr)
		{
			buffer = std::unique_ptr<uint32_t>(new uint32_t[width * height]);
			currentFrame = 0;
		}

		if(currentFrame > targetFrame)
			currentFrame = 0;
	

		uint8_t* bufPtr = (uint8_t*)buffer.get();
		uint8_t* lastFramePtr = (uint8_t*)lastFrame.get();

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				int offset = y * width + x;
				buffer.get()[offset] = *(uint32_t*)&bgColor;
			}
		}
	}

	for(int i = currentFrame; i < gifFile->ImageCount && i <= targetFrame; i++)
	{
		auto decodeFrame = frame.imageData;
		auto disposal = frame.disposal;
		auto colorMap = (decodeFrame->ImageDesc.ColorMap != nullptr ? decodeFrame->ImageDesc.ColorMap : (gifFile->SColorMap != nullptr ? gifFile->SColorMap : nullptr));

		if(disposal == DISPOSAL_METHODS::DM_PREVIOUS)
		{
			if(lastFrame == nullptr)
				lastFrame = std::unique_ptr<uint32_t>(new uint32_t[width * height]);

			memcpy(lastFrame.get(), buffer.get(), width * height * sizeof(uint32_t));
		}

		mapRasterBits(decodeFrame->RasterBits, buffer, colorMap, frame.top, frame.left, frame.bottom, frame.right, width, frame.transparentColor);
		
		switch(disposal)
		{
		case DISPOSAL_METHODS::DM_BACKGROUND:
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					int offset = y * width + x;
					buffer.get()[offset] = *(uint32_t*)&bgColor;
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
	_lastFrame = 0;
	_currentFrame = 0;
	_startedRendering = false;
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
	auto newFrame = max(i - 1, 0);
	if(newFrame != _currentFrame || _currentFrame == 0)
	{
		_currentFrame = newFrame;
		_startedRendering = true;
		return true;
	}
	else
	{
		if(!_startedRendering)
		{
			_startedRendering = true;
			return true;
		}
		else
			return false;
	}
}

void GifRenderer::Render()
{
	auto& frame = _frames[_currentFrame];
	loadGifFrame(_gifFile, frame, _buffer, _lastFrame, _currentFrame);
	_lastFrame = _currentFrame;
	loadTexture(m_d3dDevice.Get(), m_d3dContext.Get(), _buffer, frame.width, frame.height, preRendered, resource);

	if(_spriteBatch == nullptr)
		_spriteBatch = std::unique_ptr<SpriteBatch>(new DirectX::SpriteBatch(m_d3dContext.Get()));

	const float white[] = { 1.0f, 1.0f, 1.0f , 1.0f };
	m_d3dContext->ClearRenderTargetView(
		m_renderTargetView.Get(),
		white
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
	
	RECT rect = { 0, 0, frame.width, frame.height};
	_spriteBatch->Draw(resource.Get(), rect, Colors::White);
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


