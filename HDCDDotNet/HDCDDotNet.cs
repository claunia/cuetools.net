using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using AudioCodecsDotNet;

namespace HDCDDotNet
{
	/** \brief Statistics for decoding. */
	[StructLayout(LayoutKind.Sequential)]
	public struct hdcd_decoder_statistics
	{
		public UInt32 num_packets;
		/**<Total number of samples processed. */
		public bool enabled_peak_extend;
		/**< True if peak extend was enabled during decoding. */
		public bool disabled_peak_extend;
		/**< True if peak extend was disabled during decoding. */
		public double min_gain_adjustment;
		/**< Minimum dynamic gain used during decoding. */
		public double max_gain_adjustment;
		/**< Maximum dynamic gain used during decoding. */
		public bool enabled_transient_filter;
		/**< True if the transient filter was enabled during decoding. */
		public bool disabled_transient_filter;
		/**< True if the transient filter was disabled during decoding. */
	};

	public class HDCDDotNet
	{
		public HDCDDotNet (Int16 channels, Int32 sample_rate, bool decode)
		{
			_decoder = IntPtr.Zero;
#if !MONO
			_decoder = hdcd_decoder_new();
			_channelCount = channels;
			if (_decoder == IntPtr.Zero)
				throw new Exception("Failed to initialize HDCD decoder.");
			bool b = true;
			b &= hdcd_decoder_set_num_channels(_decoder, channels);
			b &= hdcd_decoder_set_sample_rate(_decoder, sample_rate);
			b &= hdcd_decoder_set_input_bps(_decoder, 16);
			b &= hdcd_decoder_set_output_bps(_decoder, 24);
			if (!b)
				throw new Exception("Failed to set up HDCD _decoder parameters.");
			_decoderCallback = decode ? new hdcd_decoder_write_callback(DecoderCallback) : null;
			_gch = GCHandle.Alloc(this);
			hdcd_decoder_init_status status = hdcd_decoder_init(_decoder, IntPtr.Zero, _decoderCallback, (IntPtr) _gch);
			switch (status)
			{
				case hdcd_decoder_init_status.HDCD_DECODER_INIT_STATUS_OK:
					break;
				case hdcd_decoder_init_status.HDCD_DECODER_INIT_STATUS_MEMORY_ALOCATION_ERROR:
					throw new Exception("Memory allocation error.");
				case hdcd_decoder_init_status.HDCD_DECODER_INIT_STATUS_INVALID_NUM_CHANNELS:
					throw new Exception("Invalid number of channels.");
				case hdcd_decoder_init_status.HDCD_DECODER_INIT_STATUS_INVALID_SAMPLE_RATE:
					throw new Exception("Invalid sample rate.");
				default:
					throw new Exception("Unknown error(" + status.ToString() + ").");
			}
#else
			throw new Exception("HDCD unsupported.");
#endif
		}

		public bool Detected
		{
			get
			{
#if !MONO
				return hdcd_decoder_detected_hdcd(_decoder);
#else
				throw new Exception("HDCD unsupported.");
#endif
			}
		}

		public int Channels
		{
			get
			{
				return _channelCount;
			}
		}

		public void Reset()
		{
#if !MONO
			if (!hdcd_decoder_reset(_decoder))
#endif
				throw new Exception("error resetting decoder.");
		}

		public void GetStatistics(out hdcd_decoder_statistics stats)
		{
#if !MONO
			IntPtr _statsPtr = hdcd_decoder_get_statistics(_decoder);
#else
			IntPtr _statsPtr = IntPtr.Zero;
#endif
			if (_statsPtr == IntPtr.Zero)
				throw new Exception("HDCD statistics error.");
			stats = (hdcd_decoder_statistics) Marshal.PtrToStructure(_statsPtr, typeof(hdcd_decoder_statistics));
		}

		public void Process(int[,] sampleBuffer, uint sampleCount)
		{
#if !MONO
			if (!hdcd_decoder_process_buffer_interleaved(_decoder, sampleBuffer, (int) sampleCount))
				throw new Exception("HDCD processing error.");
#endif
		}

		public void Process(byte[] buff, uint sampleCount)
		{
			if (_inSampleBuffer == null || _inSampleBuffer.GetLength(0) < sampleCount)
				_inSampleBuffer = new int[sampleCount, _channelCount];
			AudioSamples.BytesToFLACSamples_16(buff, 0, _inSampleBuffer, 0, sampleCount, _channelCount);
			Process(_inSampleBuffer, sampleCount);
		}

		public void Flush ()
		{
#if !MONO
			if (!hdcd_decoder_flush_buffer(_decoder))
#endif
				throw new Exception("error flushing buffer.");
		}

		public IAudioDest AudioDest
		{
			get
			{
				return _audioDest;
			}
			set
			{
				//if (hdcd_decoder_get_state(_decoder) == hdcd_decoder_state.HDCD_DECODER_STATE_DIRTY) 
				//    Flush(); /* Flushing is currently buggy! Doesn't work twice, and can't continue after flush! */
				_audioDest = value;
			}
		}

		/** \brief Return values from hdcd_decoder_init. */
		private enum hdcd_decoder_init_status
		{
			HDCD_DECODER_INIT_STATUS_OK = 0,
			/**< Initialization was successful. */
			HDCD_DECODER_INIT_STATUS_INVALID_STATE,
			/**< The _decoder was already initialised. */
			HDCD_DECODER_INIT_STATUS_MEMORY_ALOCATION_ERROR,
			/**< Initialization failed due to a memory allocation error. */
			HDCD_DECODER_INIT_STATUS_INVALID_NUM_CHANNELS,
			/**< Initialization failed because the configured number of channels was invalid. */
			HDCD_DECODER_INIT_STATUS_INVALID_SAMPLE_RATE,
			/**< Initialization failed because the configured sample rate was invalid. */
			HDCD_DECODER_INIT_STATUS_INVALID_INPUT_BPS,
			/**< Initialization failed because the configured input bits per sample was invalid. */
			HDCD_DECODER_INIT_STATUS_INVALID_OUTPUT_BPS
			/**< Initialization failed because the configured output bits per sample was invalid. */
		}

		/** \brief State values for a decoder.
		 *
		 * The decoder's state can be obtained by calling hdcd_decoder_get_state().
		 */
		private enum hdcd_decoder_state
		{
			HDCD_DECODER_STATE_UNINITIALISED = 1,
			/**< The decoder is uninitialised. */
			HDCD_DECODER_STATE_READY,
			/**< The decoder is initialised and ready to process data. */
			HDCD_DECODER_STATE_DIRTY,
			/**< The decoder has processed data, but has not yet been flushed. */
			HDCD_DECODER_STATE_FLUSHED,
			/**< The decoder has been flushed. */
			HDCD_DECODER_STATE_WRITE_ERROR,
			/**< An error was returned by the write callback. */
			HDCD_DECODER_STATE_MEMORY_ALOCATION_ERROR
			/**< Processing failed due to a memory allocation error. */
		};

		private IntPtr _decoder;
		private int[,] _inSampleBuffer;
		private int[,] _outSampleBuffer;
		private int _channelCount;
		hdcd_decoder_write_callback _decoderCallback;
		IAudioDest _audioDest;
		GCHandle _gch;

		~HDCDDotNet()
		{
#if !MONO
			if (_decoder != IntPtr.Zero) 
				hdcd_decoder_delete(_decoder);
			if (_gch.IsAllocated) 
				_gch.Free();
#endif
		}

		private delegate bool hdcd_decoder_write_callback(IntPtr decoder, IntPtr buffer, int samples, IntPtr client_data);

		private unsafe bool Output(IntPtr buffer, int samples)
		{
			if (AudioDest == null)
				return true;

			if (_outSampleBuffer == null || _outSampleBuffer.GetLength(0) < samples)
				_outSampleBuffer = new int[samples, _channelCount];

			int loopCount = samples * _channelCount;
			int* pInSamples = (int*)buffer;
			fixed (int* pOutSamplesFixed = &_outSampleBuffer[0, 0])
			{
				int* pOutSamples = pOutSamplesFixed;
				for (int i = 0; i < loopCount; i++)
					*(pOutSamples++) = *(pInSamples++);
			}
			AudioDest.Write(_outSampleBuffer, (uint)samples);
			return true;
		}

		private static unsafe bool DecoderCallback(IntPtr decoder, IntPtr buffer, int samples, IntPtr client_data)
		{
			GCHandle gch = (GCHandle)client_data;
			HDCDDotNet hdcd = (HDCDDotNet)gch.Target;
			return hdcd.Output(buffer, samples);
		}

#if !MONO
		[DllImport("hdcd.dll")] 
		private static extern IntPtr hdcd_decoder_new ();
		[DllImport("hdcd.dll")] 
		private static extern void hdcd_decoder_delete(IntPtr decoder);
		[DllImport("hdcd.dll")]
		private static extern hdcd_decoder_state hdcd_decoder_get_state(IntPtr decoder);
		[DllImport("hdcd.dll")] 
		private static extern bool hdcd_decoder_set_num_channels (IntPtr decoder, Int16 num_channels);
		//HDCD_API uint16_t hdcd_decoder_get_num_channels(const hdcd_decoder *const _decoder);
		[DllImport("hdcd.dll")] 
		private static extern bool hdcd_decoder_set_sample_rate(IntPtr decoder, Int32 sample_rate);
		//HDCD_API uint32_t hdcd_decoder_get_sample_rate(const hdcd_decoder *const _decoder);
		[DllImport("hdcd.dll")] 
		private static extern bool hdcd_decoder_set_input_bps(IntPtr decoder, Int16 input_bps);
		//HDCD_API uint16_t hdcd_decoder_get_input_bps(const hdcd_decoder *const _decoder);
		[DllImport("hdcd.dll")] 
		private static extern bool hdcd_decoder_set_output_bps(IntPtr decoder, Int16 output_bps);
		//HDCD_API uint16_t hdcd_decoder_get_output_bps(const hdcd_decoder *const _decoder);
		[DllImport("hdcd.dll")] 
		private static extern hdcd_decoder_init_status hdcd_decoder_init (IntPtr decoder, IntPtr unused, hdcd_decoder_write_callback write_callback, IntPtr client_data);
		[DllImport("hdcd.dll")] 
		private static extern bool hdcd_decoder_finish(IntPtr decoder);
		[DllImport("hdcd.dll")] 
		private static extern bool hdcd_decoder_process_buffer_interleaved(IntPtr decoder, [In, Out] int [,] input_buffer, Int32 samples);
		[DllImport("hdcd.dll")]
		private static extern bool hdcd_decoder_flush_buffer(IntPtr decoder);
		[DllImport("hdcd.dll")]
		private static extern bool hdcd_decoder_reset(IntPtr decoder);
		[DllImport("hdcd.dll")] 
		private static extern bool hdcd_decoder_detected_hdcd(IntPtr decoder);
		[DllImport("hdcd.dll")] 
		private static extern IntPtr hdcd_decoder_get_statistics(IntPtr decoder);
#endif
	}
}