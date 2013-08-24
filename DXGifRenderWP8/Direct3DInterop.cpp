#include "Direct3DInterop.h"
#include "Direct3DContentProvider.h"

using namespace Windows::Foundation;
using namespace Windows::UI::Core;
using namespace Microsoft::WRL;
using namespace Windows::Phone::Graphics::Interop;
using namespace Windows::Phone::Input::Interop;

namespace DXGifRenderWP8
{

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

	Direct3DInterop::Direct3DInterop(const Platform::Array<std::uint8_t>^ asset) :
		m_timer(ref new BasicTimer())
	{
		_gifFile = nullptr;
		int error = 0;
		gif_user_data userData = { 0, asset->Length, asset->Data };
		GifFileType* gifFile = DGifOpen(&userData, istreamReader, &error);
		if(gifFile != nullptr && DGifSlurp(gifFile) == GIF_OK)
		{
			Height = gifFile->SHeight;
			Width = gifFile->SWidth;
			_gifFile = gifFile;
		}
		else
			throw ref new Platform::InvalidArgumentException("invalid gif asset");
	}

	Direct3DInterop::~Direct3DInterop()
	{
		if(_gifFile != nullptr)
			DGifCloseFile(_gifFile);

		_gifFile = nullptr;
	}

	IDrawingSurfaceContentProvider^ Direct3DInterop::CreateContentProvider()
	{
		ComPtr<Direct3DContentProvider> provider = Make<Direct3DContentProvider>(this);
		return reinterpret_cast<IDrawingSurfaceContentProvider^>(provider.Get());
	}


	void Direct3DInterop::RenderResolution::set(Windows::Foundation::Size renderResolution)
	{
		if (renderResolution.Width  != m_renderResolution.Width ||
			renderResolution.Height != m_renderResolution.Height)
		{
			m_renderResolution = renderResolution;

			if (m_renderer)
			{
				m_renderer->UpdateForRenderResolutionChange(m_renderResolution.Width, m_renderResolution.Height);
				RecreateSynchronizedTexture();
			}
		}
	}

	// Interface With Direct3DContentProvider
	HRESULT Direct3DInterop::Connect(_In_ IDrawingSurfaceRuntimeHostNative* host)
	{
		if(m_renderer != nullptr)
		{
			delete m_renderer;
		}
		m_renderer = ref new GifRenderer(_gifFile);
		m_renderer->Initialize();
		m_renderer->UpdateForWindowSizeChange(WindowBounds.Width, WindowBounds.Height);
		m_renderer->UpdateForRenderResolutionChange(m_renderResolution.Width, m_renderResolution.Height);

		// Restart timer after renderer has finished initializing.
		m_timer->Reset();

		return S_OK;
	}

	void Direct3DInterop::Disconnect()
	{
		if(m_renderer != nullptr)
		{
			delete m_renderer;
		}
		m_renderer = nullptr;
	}

	HRESULT Direct3DInterop::PrepareResources(_In_ const LARGE_INTEGER* presentTargetTime, _Out_ BOOL* contentDirty)
	{
		*contentDirty = true;

		return S_OK;
	}

	HRESULT Direct3DInterop::GetTexture(_In_ const DrawingSurfaceSizeF* size, _Inout_ IDrawingSurfaceSynchronizedTextureNative** synchronizedTexture, _Inout_ DrawingSurfaceRectF* textureSubRectangle)
	{
		m_timer->Update();
		if(m_renderer->Update(m_timer->Total, m_timer->Delta))
			m_renderer->Render();

		RequestAdditionalFrame();

		return S_OK;
	}

	ID3D11Texture2D* Direct3DInterop::GetTexture()
	{
		return m_renderer->GetTexture();
	}

}
