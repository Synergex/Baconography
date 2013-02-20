#pragma once

#include <cstdint>
#include <wrl.h>
#include <wrl\client.h>

#include <dxgi.h>
#include <dxgi1_2.h>
#include <d2d1_1.h>
#include <d3d11_1.h>

#include <atlcomcli.h>
#include <vector>

#include <wincodec.h>

#include "windows.ui.xaml.media.dxinterop.h"

namespace DXRenderInterop
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
		int right;
		int bottom;
		int top;
		int left;
		uint32_t delay;
		DISPOSAL_METHODS disposal;
		Microsoft::WRL::ComPtr<ID2D1Bitmap> rawFrame;
		CComPtr<IWICBitmapFrameDecode> decodeFrame;
	};

	public ref class GifRenderer sealed : Windows::UI::Xaml::Media::Imaging::SurfaceImageSource
    {
    public:
		static GifRenderer^ CreateGifRenderer(const Platform::Array<std::uint8_t>^ asset);
        
		property bool Visible
		{
			bool get() { return _timer->IsEnabled; }
			void set(bool value) 
			{ 
				if(value && !_timer->IsEnabled)
					_timer->Start();
				else if(!value && _timer->IsEnabled)
					_timer->Stop();
			}
		}

		virtual ~GifRenderer()
		{
			Visible = false;
		}
    private:
		void RenderFrame(Platform::Object^ sender, Platform::Object^ arg);
		GifRenderer(CComPtr<IWICBitmapDecoder>& gifDecoder, CComPtr<IWICImagingFactory>& wicFactory, D2D1::ColorF& backgroundColor, UINT width, UINT height, int loopCount, bool hasLoop);
		static IStream* createIStreamFromArray(const Platform::Array<std::uint8_t>^ data);

        void CreateDeviceResources();

		Microsoft::WRL::ComPtr<ISurfaceImageSourceNative>   _sisNative;
        // Direct3D device
        Microsoft::WRL::ComPtr<ID3D11Device>                _d3dDevice;

        // Direct2D objects
        Microsoft::WRL::ComPtr<ID2D1Device>                 _d2dDevice;
        Microsoft::WRL::ComPtr<ID2D1DeviceContext>          _d2dContext;
		CComPtr<IWICBitmapDecoder>							_gifDecoder;
		CComPtr<IWICImagingFactory>							_wicFactory;
		Windows::UI::Xaml::DispatcherTimer^					_timer;

        int                                                 _width;
        int                                                 _height;
		bool												_hasLoop;
		int													_currentFrame;
		int													_loopCount;
		D2D1::ColorF										_backgroundColor;
		std::vector<GifFrame>								_frames;
		static void GetBackgroundColor(CComPtr<IWICMetadataQueryReader>& pMetadataQueryReader, CComPtr<IWICBitmapDecoder>& gifDecoder, CComPtr<IWICImagingFactory>& wicFactory, D2D1::ColorF& target);
		void GetRawFrame(GifFrame& frame, CComPtr<IWICBitmapFrameDecode>& frameDecode);
		void DrawRawFrame(GifFrame& frame);
		void BeginDraw(Windows::Foundation::Rect updateRect);
        void BeginDraw()    { BeginDraw(Windows::Foundation::Rect(0, 0, (float)_width, (float)_height)); }
        void EndDraw();

        void SetDpi(float dpi);

        void Clear(Windows::UI::Color color);
        void FillSolidRect(Windows::UI::Color color, Windows::Foundation::Rect rect);
    };

	

	inline void ThrowIfFailed(HRESULT hr)
    {
        if (FAILED(hr))
        {            
            throw Platform::Exception::CreateException(hr);
        }
    }

    inline D2D1_COLOR_F ConvertToColorF(Windows::UI::Color color)
    {
        return D2D1::ColorF(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);            
    }

    inline D2D1_RECT_F ConvertToRectF(Windows::Foundation::Rect source)
    {
        return D2D1::RectF(source.Left, source.Top, source.Right, source.Bottom);
    }
}