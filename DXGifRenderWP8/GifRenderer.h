#pragma once

#include "Direct3DBase.h"

#include <cstdint>
#include <wrl.h>
#include <wrl\client.h>
#include <d3d11_1.h>

#include <vector>
#include <memory>

#include <collection.h>
#include "gif_lib.h"
#include "SpriteBatch.h"


namespace DXGifRenderWP8
{
	enum DISPOSAL_METHODS
    {
        DM_UNDEFINED  = 0,
        DM_NONE       = 1,
        DM_BACKGROUND = 2,
        DM_PREVIOUS   = 3 
    };

	struct GifFrame
	{
		int width;
		int height;
		int top;
		int left;
		int right;
		int bottom;
		int transparentColor;
		uint32_t delay;
		SavedImage* imageData;
		DISPOSAL_METHODS disposal;
	};

	ref class GifRenderer sealed : Direct3DBase
	{
	internal:
		GifRenderer(GifFileType* gifFile);
	public:
		virtual void CreateDeviceResources() override;
		virtual void Render() override;
		bool Update(float total, float delta);
		bool StartedRendering() { return _startedRendering; }
	private:
		int _width;
		int _height;
		bool _hasLoop;
		int	_currentFrame;
		int	_lastFrame;
		int	_loopCount;
		bool _startedRendering;
		std::unique_ptr<DirectX::SpriteBatch> _spriteBatch;
		std::unique_ptr<uint32_t> _buffer;
		Microsoft::WRL::ComPtr<ID3D11Texture2D> preRendered;
		Microsoft::WRL::ComPtr<ID3D11ShaderResourceView> resource;
		DXGI_RGBA _backgroundColor;
		std::vector<GifFrame> _frames;
		GifFileType* _gifFile;

		void DrawRawFrame(GifFrame& frame, DirectX::SpriteBatch* batch);
	};



	inline void ThrowIfFailed(HRESULT hr)
	{
		if (FAILED(hr))
		{            
			throw Platform::Exception::CreateException(hr);
		}
	}
}
