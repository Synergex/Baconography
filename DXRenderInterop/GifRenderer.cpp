#include "GifRenderer.h"

#include "windows.ui.xaml.media.dxinterop.h"

#include <wincodec.h>
#include <wincodecsdk.h>
#pragma comment(lib, "WindowsCodecs.lib")


#include <ppltasks.h>

using namespace concurrency;
using namespace DXRenderInterop;
using Windows::UI::Xaml::Media::ImageSource;
using Windows::Storage::Streams::IInputStream;
using Windows::Storage::Streams::Buffer;
using Windows::Storage::Streams::IBuffer;
using Windows::Storage::Streams::InputStreamOptions;
using std::uint8_t;
using Microsoft::WRL::ComPtr;
using Windows::Foundation::Rect;


bool GifRenderer::_suspended;
Platform::Collections::Vector<GifRenderer^>^  GifRenderer::_activeRenderers = ref new Platform::Collections::Vector<GifRenderer^>();

// Direct3D device
Microsoft::WRL::ComPtr<ID3D11Device>                g_d3dDevice;

// Direct2D objects
Microsoft::WRL::ComPtr<ID2D1Device>                 g_d2dDevice;


CComPtr<IWICImagingFactory>							g_wicFactory;

GifRenderer^ GifRenderer::CreateGifRenderer(const Platform::Array<std::uint8_t>^ asset)
{
	try
	{
		if(g_wicFactory == nullptr)
		{
			ThrowIfFailed(CoCreateInstance(
				CLSID_WICImagingFactory,
				nullptr,
				CLSCTX_INPROC_SERVER,
				IID_PPV_ARGS(&g_wicFactory)
				));
		}

	

		ComPtr<IStream> stream;
		createIStreamFromArray(asset, &stream);
		CComPtr<IWICBitmapDecoder> gifDecoder;
		if(g_wicFactory->CreateDecoderFromStream(stream.Get(), NULL, WICDecodeOptions::WICDecodeMetadataCacheOnDemand, &gifDecoder) != S_OK)
			return nullptr;

		UINT width = 0, adjustedWidth = 0;
		UINT height = 0, adjustedHeight = 0;
		int loopCount = 0;
		bool hasLoop = false;

		 PROPVARIANT propValue;
		PropVariantInit(&propValue);
		CComPtr<IWICMetadataQueryReader> pMetadataQueryReader;

		// Create a MetadataQueryReader from the decoder
		if(FAILED(gifDecoder->GetMetadataQueryReader(&pMetadataQueryReader)))
			throw ref new Platform::FailureException();
    

		D2D1::ColorF backgroundColor(0, 0.f);
		GifRenderer::GetBackgroundColor(pMetadataQueryReader, gifDecoder, g_wicFactory, backgroundColor);
    
		// Get width
		if(FAILED(pMetadataQueryReader->GetMetadataByName(
			L"/logscrdesc/Width", 
			&propValue)))
			throw ref new Platform::FailureException();

    
		if (SUCCEEDED((propValue.vt == VT_UI2 ? S_OK : E_FAIL)))
		{
			width = propValue.uiVal;
		}
		PropVariantClear(&propValue);
    
    
		// Get height
		if(FAILED(pMetadataQueryReader->GetMetadataByName(
			L"/logscrdesc/Height",
			&propValue)))
			throw ref new Platform::FailureException();

		if (SUCCEEDED((propValue.vt == VT_UI2 ? S_OK : E_FAIL)))
		{
			height = propValue.uiVal;
		}
		PropVariantClear(&propValue);
    
		// Get pixel aspect ratio
		HRESULT hr = pMetadataQueryReader->GetMetadataByName(
			L"/logscrdesc/PixelAspectRatio",
			&propValue);
		if (SUCCEEDED(hr))
		{
			hr = (propValue.vt == VT_UI1 ? S_OK : E_FAIL);
			if (SUCCEEDED(hr))
			{
				UINT uPixelAspRatio = propValue.bVal;

				if (uPixelAspRatio != 0)
				{
					// Need to calculate the ratio. The value in uPixelAspRatio 
					// allows specifying widest pixel 4:1 to the tallest pixel of 
					// 1:4 in increments of 1/64th
					FLOAT pixelAspRatio = (uPixelAspRatio + 15.f) / 64.f;

					// Calculate the image width and height in pixel based on the
					// pixel aspect ratio. Only shrink the image.
					if (pixelAspRatio > 1.f)
					{
						adjustedHeight = height;
						adjustedWidth = static_cast<UINT>(width / pixelAspRatio);
					}
					else
					{
						adjustedHeight = static_cast<UINT>(height * pixelAspRatio);
						adjustedWidth = width;
					}
				}
				else
				{
					// The value is 0, so its ratio is 1
					adjustedHeight = height;
					adjustedWidth = width;
				}
			}
			PropVariantClear(&propValue);
		}

		// Get looping information
    
		// First check to see if the application block in the Application Extension
		// contains "NETSCAPE2.0" and "ANIMEXTS1.0", which indicates the gif animation
		// has looping information associated with it.
		// 
		// If we fail to get the looping information, loop the animation infinitely.
		if (SUCCEEDED(pMetadataQueryReader->GetMetadataByName(
					L"/appext/application", 
					&propValue)) &&
			propValue.vt == (VT_UI1 | VT_VECTOR) &&
			propValue.caub.cElems == 11 &&  // Length of the application block
			(!memcmp(propValue.caub.pElems, "NETSCAPE2.0", propValue.caub.cElems) ||
				!memcmp(propValue.caub.pElems, "ANIMEXTS1.0", propValue.caub.cElems)))
		{
			PropVariantClear(&propValue);

			hr = pMetadataQueryReader->GetMetadataByName(L"/appext/data", &propValue);
			if (SUCCEEDED(hr))
			{
				//  The data is in the following format:
				//  byte 0: extsize (must be > 1)
				//  byte 1: loopType (1 == animated gif)
				//  byte 2: loop count (least significant byte)
				//  byte 3: loop count (most significant byte)
				//  byte 4: set to zero
				if (propValue.vt == (VT_UI1 | VT_VECTOR) &&
					propValue.caub.cElems >= 4 &&
					propValue.caub.pElems[0] > 0 &&
					propValue.caub.pElems[1] == 1)
				{
					loopCount = MAKEWORD(propValue.caub.pElems[2], 
						propValue.caub.pElems[3]);
                    
					// If the total loop count is not zero, we then have a loop count
					// If it is 0, then we repeat infinitely
					if (loopCount != 0) 
					{
						hasLoop = true;
					}
				}
			}
		}

		return ref new GifRenderer(gifDecoder, backgroundColor, adjustedWidth, adjustedHeight, loopCount, hasLoop);
	}
	catch(...)
	{
		return nullptr;
	}
}

GifRenderer::GifRenderer(CComPtr<IWICBitmapDecoder>& gifDecoder, 
						 D2D1::ColorF& backgroundColor, UINT width, UINT height, int loopCount, bool hasLoop) 
						 : Windows::UI::Xaml::Media::Imaging::SurfaceImageSource(width, height), _backgroundColor(backgroundColor), 
						 _loopCount(loopCount), _hasLoop(hasLoop), _gifDecoder(gifDecoder),
						 _width(width), _height(height)
{
	reinterpret_cast<IUnknown*>(this)->QueryInterface(IID_PPV_ARGS(&_sisNative));
	CreateDeviceResources();
	_currentFrame = 0;
	_timer = ref new Windows::UI::Xaml::DispatcherTimer();
	_activeRenderers->Append(this);
	Windows::Foundation::TimeSpan nextFrameIn = { 90 * 1000 };
	_timer->Interval = nextFrameIn;
	_timer->Tick += ref new Windows::Foundation::EventHandler<Platform::Object^>(this, &GifRenderer::RenderFrame);
}

void GifRenderer::GetRawFrame(GifFrame& frame, CComPtr<IWICBitmapFrameDecode>& frameDecode)
{
	frame.decodeFrame = frameDecode;
	CComPtr<IWICFormatConverter> pConverter;
    CComPtr<IWICMetadataQueryReader> pFrameMetadataQueryReader;
    
    PROPVARIANT propValue;
    PropVariantInit(&propValue);

    
    // Format convert to 32bppPBGRA which D2D expects
	HRESULT hr = g_wicFactory->CreateFormatConverter(&pConverter);
    

    if (SUCCEEDED(hr))
    {
        hr = pConverter->Initialize(
            frameDecode,
            GUID_WICPixelFormat32bppPBGRA,
            WICBitmapDitherTypeNone,
            NULL,
            0.f,
            WICBitmapPaletteTypeCustom);
    }

	ThrowIfFailed(_d2dContext->CreateBitmapFromWicBitmap(pConverter, nullptr, &frame.rawFrame));

    if (SUCCEEDED(hr))
    {
        // Get Metadata Query Reader from the frame
        hr = frameDecode->GetMetadataQueryReader(&pFrameMetadataQueryReader);
    }

    // Get the Metadata for the current frame
    if (SUCCEEDED(hr))
    {
        hr = pFrameMetadataQueryReader->GetMetadataByName(L"/imgdesc/Left", &propValue);
        if (SUCCEEDED(hr))
        {
            hr = (propValue.vt == VT_UI2 ? S_OK : E_FAIL); 
            if (SUCCEEDED(hr))
            {
                frame.left = static_cast<FLOAT>(propValue.uiVal);
            }
            PropVariantClear(&propValue);
        }
    }

    if (SUCCEEDED(hr))
    {
        hr = pFrameMetadataQueryReader->GetMetadataByName(L"/imgdesc/Top", &propValue);
        if (SUCCEEDED(hr))
        {
            hr = (propValue.vt == VT_UI2 ? S_OK : E_FAIL); 
            if (SUCCEEDED(hr))
            {
                frame.top = static_cast<FLOAT>(propValue.uiVal);
            }
            PropVariantClear(&propValue);
        }
    }

    if (SUCCEEDED(hr))
    {
        hr = pFrameMetadataQueryReader->GetMetadataByName(L"/imgdesc/Width", &propValue);
        if (SUCCEEDED(hr))
        {
            hr = (propValue.vt == VT_UI2 ? S_OK : E_FAIL); 
            if (SUCCEEDED(hr))
            {
                frame.right = static_cast<FLOAT>(propValue.uiVal) 
                    + frame.left;
            }
            PropVariantClear(&propValue);
        }
    }

    if (SUCCEEDED(hr))
    {
        hr = pFrameMetadataQueryReader->GetMetadataByName(L"/imgdesc/Height", &propValue);
        if (SUCCEEDED(hr))
        {
            hr = (propValue.vt == VT_UI2 ? S_OK : E_FAIL);
            if (SUCCEEDED(hr))
            {
                frame.bottom = static_cast<FLOAT>(propValue.uiVal)
                    + frame.top;
            }
            PropVariantClear(&propValue);
        }
    }

    if (SUCCEEDED(hr))
    {
        // Get delay from the optional Graphic Control Extension
        if (SUCCEEDED(pFrameMetadataQueryReader->GetMetadataByName(
            L"/grctlext/Delay", 
            &propValue)))
        {
            hr = (propValue.vt == VT_UI2 ? S_OK : E_FAIL); 
            if (SUCCEEDED(hr))
            {
                // Convert the delay retrieved in 10 ms units to a delay in 1 ms units
                hr = UIntMult(propValue.uiVal, 10, &frame.delay);
            }
            PropVariantClear(&propValue);
        }
        else
        {
            // Failed to get delay from graphic control extension. Possibly a
            // single frame image (non-animated gif)
            frame.delay = 0;
        }

        if (SUCCEEDED(hr))
        {
            // Insert an artificial delay to ensure rendering for gif with very small
            // or 0 delay.  This delay number is picked to match with most browsers' 
            // gif display speed.
            //
            // This will defeat the purpose of using zero delay intermediate frames in 
            // order to preserve compatibility. If this is removed, the zero delay 
            // intermediate frames will not be visible.
            if (frame.delay < 90)
            {
                frame.delay = 90;
            }
        }
    }

	if (SUCCEEDED(hr))
    {
        if (SUCCEEDED(pFrameMetadataQueryReader->GetMetadataByName(
                L"/grctlext/Disposal", 
                &propValue)))
        {
            hr = (propValue.vt == VT_UI1) ? S_OK : E_FAIL;
            if (SUCCEEDED(hr))
            {
				frame.disposal = (DISPOSAL_METHODS)propValue.bVal;
            }
        }
        else
        {
            // Failed to get the disposal method, use default. Possibly a 
            // non-animated gif.
            frame.disposal = DM_UNDEFINED;
        }
    }
}

void GifRenderer::CreateDeviceResources()
{
	if(g_d3dDevice == nullptr)
	{
		// This flag adds support for surfaces with a different color channel ordering
		// than the API default. It is required for compatibility with Direct2D.
		UINT creationFlags = D3D11_CREATE_DEVICE_BGRA_SUPPORT; 

	#if defined(_DEBUG)    
		// If the project is in a debug build, enable debugging via SDK Layers.
		creationFlags |= D3D11_CREATE_DEVICE_DEBUG;
	#endif

		// This array defines the set of DirectX hardware feature levels this app will support.
		// Note the ordering should be preserved.
		// Don't forget to declare your application's minimum required feature level in its
		// description.  All applications are assumed to support 9.1 unless otherwise stated.
		const D3D_FEATURE_LEVEL featureLevels[] =
		{
			D3D_FEATURE_LEVEL_11_1,
			D3D_FEATURE_LEVEL_11_0,
			D3D_FEATURE_LEVEL_10_1,
			D3D_FEATURE_LEVEL_10_0,
			D3D_FEATURE_LEVEL_9_3,
			D3D_FEATURE_LEVEL_9_2,
			D3D_FEATURE_LEVEL_9_1,
		};

		// Create the Direct3D 11 API device object.
		ThrowIfFailed(
			D3D11CreateDevice(
				nullptr,                        // Specify nullptr to use the default adapter.
				D3D_DRIVER_TYPE_HARDWARE,
				nullptr,
				creationFlags,                  // Set debug and Direct2D compatibility flags.
				featureLevels,                  // List of feature levels this app can support.
				ARRAYSIZE(featureLevels),
				D3D11_SDK_VERSION,              // Always set this to D3D11_SDK_VERSION for Metro style apps.
				&g_d3dDevice,                   // Returns the Direct3D device created.
				nullptr,
				nullptr
				)
			);

		// Get the Direct3D 11.1 API device.
		ComPtr<IDXGIDevice> dxgiDevice;
		ThrowIfFailed(
			g_d3dDevice.As(&dxgiDevice)
			);

		// Create the Direct2D device object and a corresponding context.
		ThrowIfFailed(
			D2D1CreateDevice(
				dxgiDevice.Get(),
				nullptr,
				&g_d2dDevice
				)
			);
	}

	ThrowIfFailed(
		g_d2dDevice->CreateDeviceContext(
			D2D1_DEVICE_CONTEXT_OPTIONS_NONE,
			&_d2dContext
			)
		);

	// Set DPI to the display's current DPI.
	SetDpi(Windows::Graphics::Display::DisplayProperties::LogicalDpi);

	// Get the Direct3D 11.1 API device.
	ComPtr<IDXGIDevice> dxgiDevice2;
	ThrowIfFailed(
		g_d3dDevice.As(&dxgiDevice2)
		);

    // Associate the DXGI device with the SurfaceImageSource.
    ThrowIfFailed(
		_sisNative->SetDevice(dxgiDevice2.Get())
        );


	UINT frameCount = 0;
	if(FAILED(_gifDecoder->GetFrameCount(&frameCount)))
		throw ref new Platform::FailureException();

	_frames.resize(frameCount);

	for (UINT index = 0; index < frameCount; ++index)
	{
		CComPtr<IWICBitmapFrameDecode> frame;
		if(FAILED(_gifDecoder->GetFrame(index, &frame)))
			throw ref new Platform::FailureException();

		GetRawFrame(_frames[index], frame);
	}
}

void GifRenderer::BeginDraw(Rect updateRect)
{
	POINT offset;
    ComPtr<IDXGISurface> surface;

    // Express target area as a native RECT type.
    RECT updateRectNative; 
    updateRectNative.left = static_cast<LONG>(updateRect.Left);
    updateRectNative.top = static_cast<LONG>(updateRect.Top);
    updateRectNative.right = static_cast<LONG>(updateRect.Right);
    updateRectNative.bottom = static_cast<LONG>(updateRect.Bottom);

    // Begin drawing - returns a target surface and an offset to use as the top left origin when drawing.
    HRESULT beginDrawHR = _sisNative->BeginDraw(updateRectNative, &surface, &offset);
 
    if (beginDrawHR == DXGI_ERROR_DEVICE_REMOVED || beginDrawHR == DXGI_ERROR_DEVICE_RESET)
    {
        // If the device has been removed or reset, attempt to recreate it and continue drawing.
        CreateDeviceResources();
        BeginDraw(updateRect);
    }
    else
    {
        // Notify the caller by throwing an exception if any other error was encountered.
        ThrowIfFailed(beginDrawHR);
    }


	// Create render target.
	ComPtr<ID2D1Bitmap1> bitmap;
	
	ThrowIfFailed(
		_d2dContext->CreateBitmapFromDxgiSurface(
			surface.Get(),
			nullptr,
			&bitmap
			)
		);

	// Set context's render target.
	_d2dContext->SetTarget(bitmap.Get());
	

    // Begin drawing using D2D context.
    _d2dContext->BeginDraw();

    // Apply a clip and transform to constrain updates to the target update area.
    // This is required to ensure coordinates within the target surface remain
    // consistent by taking into account the offset returned by BeginDraw, and
    // can also improve performance by optimizing the area that is drawn by D2D.
    // Apps should always account for the offset output parameter returned by 
    // BeginDraw, since it may not match the passed updateRect input parameter's location.
    _d2dContext->PushAxisAlignedClip(
        D2D1::RectF(
            static_cast<float>(offset.x),  
            static_cast<float>(offset.y),  
            static_cast<float>(offset.x + updateRect.Width),
            static_cast<float>(offset.y + updateRect.Height)  
            ),  
        D2D1_ANTIALIAS_MODE_ALIASED  
        );

    _d2dContext->SetTransform(
        D2D1::Matrix3x2F::Translation(
            static_cast<float>(offset.x),
            static_cast<float>(offset.y)
            )
        );
}


void GifRenderer::GetBackgroundColor(CComPtr<IWICMetadataQueryReader>& pMetadataQueryReader, CComPtr<IWICBitmapDecoder>& gifDecoder, CComPtr<IWICImagingFactory>& wicFactory, D2D1::ColorF& target)
{
    DWORD dwBGColor;
    BYTE backgroundIndex = 0;
    WICColor rgColors[256];
    UINT cColorsCopied = 0;
    PROPVARIANT propVariant;
    PropVariantInit(&propVariant);
    CComPtr<IWICPalette> pWicPalette;

    // If we have a global palette, get the palette and background color
    HRESULT hr = pMetadataQueryReader->GetMetadataByName(
        L"/logscrdesc/GlobalColorTableFlag",
        &propVariant);
    if (SUCCEEDED(hr))
    {
        hr = (propVariant.vt != VT_BOOL || !propVariant.boolVal) ? E_FAIL : S_OK;
        PropVariantClear(&propVariant);
    }

    if (SUCCEEDED(hr))
    {
        // Background color index
        hr = pMetadataQueryReader->GetMetadataByName(
            L"/logscrdesc/BackgroundColorIndex", 
            &propVariant);
        if (SUCCEEDED(hr))
        {
            hr = (propVariant.vt != VT_UI1) ? E_FAIL : S_OK;
            if (SUCCEEDED(hr))
            {
                backgroundIndex = propVariant.bVal;
            }
            PropVariantClear(&propVariant);
        }
    }

    // Get the color from the palette
    if (SUCCEEDED(hr))
    {
        hr = wicFactory->CreatePalette(&pWicPalette.p);
    }

    if (SUCCEEDED(hr))
    {
        // Get the global palette
        hr = gifDecoder->CopyPalette(pWicPalette.p);
    }

    if (SUCCEEDED(hr))
    {
        hr = pWicPalette->GetColors(
            ARRAYSIZE(rgColors),
            rgColors,
            &cColorsCopied);
    }

    if (SUCCEEDED(hr))
    {
        // Check whether background color is outside range 
        hr = (backgroundIndex >= cColorsCopied) ? E_FAIL : S_OK;
    }

    if (SUCCEEDED(hr))
    {
        // Get the color in ARGB format
        dwBGColor = rgColors[backgroundIndex];

        // The background color is in ARGB format, and we want to 
        // extract the alpha value and convert it in FLOAT
        FLOAT alpha = (dwBGColor >> 24) / 255.f;
        target = D2D1::ColorF(dwBGColor, alpha);
    }
}

void GifRenderer::DrawRawFrame(GifFrame& frame)
{
	D2D1_RECT_F renderRect = { frame.left, frame.top, frame.right, frame.bottom }; //left, top, right, bottom
	_d2dContext->DrawBitmap(frame.rawFrame.Get(), &renderRect);
}

void GifRenderer::RenderFrame(Platform::Object^ sender, Platform::Object^ arg)
{
	//fill the image with background color
	//draw all frames marked as 'background'
	//draw next frame
	//wait 'next frame's delay


	//we're dead stop trying
	if(_sisNative == nullptr)
	{
		_timer->Stop();
		return;
	}

	Windows::Foundation::TimeSpan nextFrameIn = { _frames[_currentFrame].delay * 1000 };

	if(nextFrameIn.Duration == 0)
	{
		_timer->Stop();
	}
	else if(_timer->Interval.Duration != nextFrameIn.Duration)
	{
		_timer->Stop();
		_timer->Interval = nextFrameIn;
		_timer->Start();
	}

	if(_frames[_currentFrame].preRendered != nullptr)
	{
		try
		{
			BeginDraw();
			D2D1_RECT_F renderRect = { 0, 0, _width, _height }; //left, top, right, bottom
			_d2dContext->DrawBitmap(_frames[_currentFrame].preRendered.Get(), &renderRect);
			_d2dContext->SetTransform(D2D1::IdentityMatrix());
			_d2dContext->PopAxisAlignedClip();
			EndDraw();
		}
		catch(...)
		{
			try
			{
				EndDraw();
			}
			catch(...)
			{
			}
		}
	}
	else
	{
		try
		{
			BeginDraw();
			int renderFrom = _currentFrame;
			for(int i = _currentFrame; i >= 0; i--)
			{
				if(_frames[i].disposal == DISPOSAL_METHODS::DM_BACKGROUND)
				{
					renderFrom = i;
					break;
				}
				else if(_frames[i].disposal == DISPOSAL_METHODS::DM_UNDEFINED)
					break;
				else
					renderFrom = i;
			}

			for(int i = renderFrom; i <= _currentFrame; i++)
			{
				if(_frames[i].disposal == DISPOSAL_METHODS::DM_BACKGROUND)
				{
					_d2dContext->Clear(&_backgroundColor);
					continue;
				}

				if(i != _currentFrame && _frames[i].disposal == DISPOSAL_METHODS::DM_PREVIOUS)
					continue;
				else
					DrawRawFrame(_frames[i]);
			}

			D2D1_POINT_2U zeroPoint = { 0, 0 };
			D2D1_SIZE_U bitmapSize = { (unsigned)_width, (unsigned)_height };
			D2D1_RECT_U bitmapRect = { 0 , 0, (unsigned)_width, (unsigned)_height };
			D2D1_BITMAP_PROPERTIES prop;

			prop.pixelFormat = D2D1::PixelFormat(
			DXGI_FORMAT_B8G8R8A8_UNORM,
			D2D1_ALPHA_MODE_PREMULTIPLIED
			);

			prop.dpiX = 96;
			prop.dpiY = 96;

		

			ThrowIfFailed(_d2dContext->CreateBitmap(bitmapSize, prop, &_frames[_currentFrame].preRendered));

			_d2dContext->SetTransform(D2D1::IdentityMatrix());
			_d2dContext->PopAxisAlignedClip();

			ThrowIfFailed(_frames[_currentFrame].preRendered->CopyFromRenderTarget(nullptr, _d2dContext.Get(), nullptr));
			EndDraw();
		}
		catch(...)
		{
			try
			{
				EndDraw();
			}
			catch(...)
			{
			}
		}
	}

	_currentFrame++;
	if(_currentFrame >= _frames.size())
	{
		_currentFrame = 0;
		for(int i = 0; i < _frames.size(); i++)
		{
			_frames[_currentFrame].rawFrame = nullptr;
		}
	}
}

void GifRenderer::EndDraw()
{
	// Remove the transform and clip applied in BeginDraw since
    // the target area can change on every update.
    
    //_d2dContext->PopAxisAlignedClip();

    // Remove the render target and end drawing.
    ThrowIfFailed(
        _d2dContext->EndDraw()
        );

    _d2dContext->SetTarget(nullptr);

    ThrowIfFailed(
        _sisNative->EndDraw()
        );
}

void GifRenderer::SetDpi(float dpi)
{
	_d2dContext->SetDpi(dpi, dpi);
}

void GifRenderer::Clear(Windows::UI::Color color)
{
	_d2dContext->Clear(ConvertToColorF(color));
}

void GifRenderer::FillSolidRect(Windows::UI::Color color, Windows::Foundation::Rect rect)
{
	// Create a solid color D2D brush.
    ComPtr<ID2D1SolidColorBrush> brush;
    ThrowIfFailed(
        _d2dContext->CreateSolidColorBrush(
            ConvertToColorF(color),
            &brush
            )
        );

    // Draw a filled rectangle.
    _d2dContext->FillRectangle(ConvertToRectF(rect), brush.Get());
}

void GifRenderer::createIStreamFromArray(const Platform::Array<std::uint8_t>^ data, IStream** result)
{
    HRESULT res = CreateStreamOnHGlobal(NULL, TRUE, result);
    if (FAILED(res) || !*result) 
		throw ref new Platform::FailureException();
    
	ULONG written;
	res = (*result)->Write(data->Data, data->Length, &written);

	if (FAILED(res) || written != data->Length)
	{
        (*result)->Release();
        throw ref new Platform::FailureException();
    }
}