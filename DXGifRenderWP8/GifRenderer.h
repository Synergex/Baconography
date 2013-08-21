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
		uint32_t delay;
		Microsoft::WRL::ComPtr<ID3D11Texture2D> preRendered;
		Microsoft::WRL::ComPtr<ID3D11ShaderResourceView> resource;
	};

	ref class GifRenderer sealed : Direct3DBase
	{
	internal:
		GifRenderer(GifFileType* gifFile);
	public:
		virtual void CreateDeviceResources() override;
		virtual void Render() override;
		bool Update(float total, float delta);
	private:
		int _width;
		int _height;
		bool _hasLoop;
		int	_currentFrame;
		int	_loopCount;
		std::unique_ptr<DirectX::SpriteBatch> _spriteBatch;
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
