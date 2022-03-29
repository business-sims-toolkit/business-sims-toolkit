using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;
using Algorithms;
using DirectShowLib;

namespace Media
{
	public class DirectShowMediaPanel : MediaPanel
	{
		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool DuplicateHandle (IntPtr hSourceProcessHandle,
		                                    IntPtr hSourceHandle,
		                                    IntPtr hTargetProcessHandle,
		                                    out IntPtr lpTargetHandle,
			                                uint dwDesiredAccess,
		                                    [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
											[MarshalAs(UnmanagedType.U4)] DuplicateHandleOptions dwOptions);

		[Flags]
		public enum DuplicateHandleOptions : uint
		{
			DUPLICATE_CLOSE_SOURCE = (0x00000001), // Closes the source handle. This occurs regardless of any error status returned.
			DUPLICATE_SAME_ACCESS = (0x00000002),  //Ignores the dwDesiredAccess parameter. The duplicate handle has the same access as the source handle.
		}

		class AudioFilterSavedState : IDisposable
		{
			readonly IFilterGraph filterGraph;
			readonly IMediaControl control;

			readonly IBaseFilter audioFilter;
			readonly string audioFilterName;

			public AudioFilterSavedState (IFilterGraph filterGraph, IMediaControl control, bool playing)
			{
				this.filterGraph = filterGraph;
				this.control = control;

				audioFilter = null;
				var filters = new IBaseFilter [1];
				IEnumFilters enumFilters;
				filterGraph.EnumFilters(out enumFilters);
				while (enumFilters.Next(filters.Length, filters, IntPtr.Zero) == 0)
				{
					FilterInfo filterInfo;
					filters[0].QueryFilterInfo(out filterInfo);

					if (filters[0] is IBasicAudio)
					{
						if (playing)
						{
							control.Stop();
						}

						audioFilter = filters[0];
						audioFilterName = filterInfo.achName;

						filterGraph.RemoveFilter(audioFilter);
						if (playing)
						{
							control.Run();
						}

						break;
					}
				}
			}

			public void Dispose ()
			{
			}

			public void Restore (bool playing)
			{
				if (audioFilter != null)
				{
					if (playing)
					{
						control.Stop();
					}

					filterGraph.AddFilter(audioFilter, audioFilterName);

					if (playing)
					{
						control.Run();
					}
				}
			}
		}

		string filename;
		MediaState state;

		IFilterGraph filterGraph;
		IGraphBuilder graphBuilder;
		IMediaControl mediaControl;
		IMediaEventEx mediaEvent;
		IMediaSeeking mediaSeeking;
		IMediaPosition mediaPosition;
		IBasicVideo basicVideo;
		IBasicAudio basicAudio;
		VideoMixingRenderer9 vmr;
		IVMRFilterConfig9 vmrConfig;
		IVMRWindowlessControl9 vmrControl;

		AudioFilterSavedState audioSavedState;

		bool hasAudio;
		bool hasVideo;

		ManualResetEvent resetEvent;
		Thread eventPollingThread;

		bool playLooped;

		VideoInfoHeader videoHeader;

		readonly TransparentPanel transparentPanel;

		static bool throwOnSpeedErrors;

		Image image;

		bool waitingForEventThreadToDie;

        public bool Muted { get; set; }

		static DirectShowMediaPanel ()
		{
			throwOnSpeedErrors = false;
		}

		public DirectShowMediaPanel ()
		{
			state = MediaState.Unloaded;
			Application.ApplicationExit += Application_ApplicationExit;

			audioSavedState = null;
			image = null;

			transparentPanel = new TransparentPanel ();

			transparentPanel.Click += transparentPanel_Click;
			transparentPanel.DragDrop += transparentPanel_DragDrop;
			transparentPanel.DoubleClick += transparentPanel_DoubleClick;
			transparentPanel.DragEnter += transparentPanel_DragEnter;
			transparentPanel.DragLeave += transparentPanel_DragLeave;
			transparentPanel.DragOver += transparentPanel_DragOver;
			transparentPanel.KeyDown += transparentPanel_KeyDown;
			transparentPanel.KeyPress += transparentPanel_KeyPress;
			transparentPanel.KeyUp += transparentPanel_KeyUp;
			transparentPanel.MouseClick += transparentPanel_MouseClick;
			transparentPanel.MouseDoubleClick += transparentPanel_MouseDoubleClick;
			transparentPanel.MouseDown += transparentPanel_MouseDown;
			transparentPanel.MouseEnter += transparentPanel_MouseEnter;
			transparentPanel.MouseHover += transparentPanel_MouseHover;
			transparentPanel.MouseLeave += transparentPanel_MouseLeave;
			transparentPanel.MouseMove += transparentPanel_MouseMove;
			transparentPanel.MouseUp += transparentPanel_MouseUp;
			transparentPanel.MouseWheel += transparentPanel_MouseWheel;
			Controls.Add(transparentPanel);

			Microsoft.Win32.SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;

			SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);

			DoSize();
		}

		void Application_ApplicationExit (object sender, EventArgs e)
		{
			UnloadMedia();
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				Application.ApplicationExit -= Application_ApplicationExit;
				Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;

				UnloadMedia();
				transparentPanel.Dispose();

				waitingForEventThreadToDie = true;
			}

			base.Dispose(disposing);
		}

		static IBaseFilter GetAudioFilter (IFilterGraph filterGraph)
		{
			IEnumFilters enumFilters;
			filterGraph.EnumFilters(out enumFilters);

			var filters = new IBaseFilter [1];
			while (enumFilters.Next(filters.Length, filters, IntPtr.Zero) == 0)
			{
				if (filters[0] is IBasicAudio)
				{
					return filters[0];
				}
			}

			return null;
		}

		IPin GetInputPin (IBaseFilter filter)
		{
			IEnumPins enumPins;
			filter.EnumPins(out enumPins);
			var pins = new IPin [1];
			while (enumPins.Next(pins.Length, pins, IntPtr.Zero) == 0)
			{
				IPin pin = pins[0];
				PinDirection direction;
				pin.QueryDirection(out direction);

				if (direction == PinDirection.Input)
				{
					IPin connection;
					pin.ConnectedTo(out connection);

					if (connection != null)
					{
						return pin;
					}
				}
			}

			return null;
		}

		VideoInfoHeader GetVideoFormat (IPin inputPin)
		{
			if (inputPin != null)
			{
				var mediaType = new AMMediaType ();

				try
				{
					DsError.ThrowExceptionForHR(inputPin.ConnectionMediaType(mediaType));

					videoHeader = new VideoInfoHeader ();
					Marshal.PtrToStructure(mediaType.formatPtr, videoHeader);
					return videoHeader;
				}
				finally
				{
					DsUtils.FreeAMMediaType(mediaType);
				}
			}

			return null;
		}

		public override void LoadMedia (string filename)
		{
			if ((state != MediaState.Unloaded)
				|| (image != null))
			{
				UnloadMedia();
			}

			try
			{
				this.filename = filename;

				switch (System.IO.Path.GetExtension(filename).ToLowerInvariant())
				{
					case ".png":
					case ".gif":
					case ".bmp":
					case ".jpg":
					case ".jpeg":
						image = Image.FromFile(filename);
						state = MediaState.Paused;
						Invalidate();
						return;
				}

				filterGraph = (IFilterGraph) new FilterGraph ();
				graphBuilder = (IGraphBuilder) filterGraph;

				vmr = new VideoMixingRenderer9 ();

				vmrConfig = (IVMRFilterConfig9) vmr;
				DsError.ThrowExceptionForHR(vmrConfig.SetRenderingMode(VMR9Mode.Windowless));

				vmrControl = (IVMRWindowlessControl9) vmr;
				DsError.ThrowExceptionForHR(vmrControl.SetVideoClippingWindow(Handle));
				DsError.ThrowExceptionForHR(vmrControl.SetAspectRatioMode(VMR9AspectRatioMode.None));

				graphBuilder.AddFilter((IBaseFilter) vmr, "Renderer");
				DsError.ThrowExceptionForHR(graphBuilder.RenderFile(filename, null));

				mediaControl = (IMediaControl) graphBuilder;
				mediaEvent = (IMediaEventEx) graphBuilder;
				mediaSeeking = (IMediaSeeking) graphBuilder;
				mediaPosition = (IMediaPosition) graphBuilder;
				basicVideo = graphBuilder as IBasicVideo;
				basicAudio = graphBuilder as IBasicAudio;

				hasVideo = (basicVideo != null);
				hasAudio = (basicAudio != null);

				IntPtr theirEventHandle;
				IntPtr eventHandle;
				DsError.ThrowExceptionForHR(mediaEvent.GetEventHandle(out theirEventHandle));

				DuplicateHandle(System.Diagnostics.Process.GetCurrentProcess().Handle,
				                theirEventHandle,
				                System.Diagnostics.Process.GetCurrentProcess().Handle,
				                out eventHandle,
				                0,
				                true,
				                DuplicateHandleOptions.DUPLICATE_SAME_ACCESS);

				resetEvent = new ManualResetEvent (false)
					             {
						             SafeWaitHandle = new Microsoft.Win32.SafeHandles.SafeWaitHandle (eventHandle, true)
					             };

				eventPollingThread = new Thread (PollEvents)
					                     {
						                     Name = "Media event polling thread",
											 IsBackground = true
					                     };
				waitingForEventThreadToDie = false;
				eventPollingThread.Start();

				if (hasVideo)
				{
					try
					{
						videoHeader = GetVideoFormat(GetInputPin((IBaseFilter) vmr));
						if (videoHeader == null)
						{
							hasVideo = false;
						}
					}
					catch
					{
						hasVideo = false;
					}
				}

				if (hasAudio)
				{
					try
					{
						if (GetAudioFilter(filterGraph) == null)
						{
							hasAudio = false;
						}

						// Read the volume -- if it throws an exception, we have no audio.
						int volume;
						basicAudio.get_Volume(out volume);
						if (volume == 0)
						{
							hasAudio = false;
						}
					}
					catch
					{
						hasAudio = false;
					}
				}

				state = MediaState.Paused;

				DoSize();
			}
			catch
			{
				UnloadMedia();

				if (! SuppressFileErrors)
				{
					throw;
				}
			}
		}

		public override void UnloadMedia ()
		{
			if (audioSavedState != null)
			{
				audioSavedState.Dispose();
				audioSavedState = null;
			}

			if (mediaControl != null)
			{
				mediaControl.Stop();
			}

			if (resetEvent != null)
			{
				if ((eventPollingThread != null)
					&& eventPollingThread.IsAlive)
				{
					waitingForEventThreadToDie = true;
					resetEvent.Set();

					DateTime start = DateTime.Now;
					while (waitingForEventThreadToDie
					       && ((DateTime.Now - start).TotalSeconds < 2))
					{
						System.Threading.Thread.Sleep(100);
					}

					if (waitingForEventThreadToDie
						&& (eventPollingThread != null))
					{
						eventPollingThread.Abort();

						while (! ((eventPollingThread.ThreadState == ThreadState.Stopped)
						         || (eventPollingThread.ThreadState == ThreadState.Aborted)))
						{
						}
					}
				}

				eventPollingThread = null;

                if (resetEvent != null)
                {
                    resetEvent.Dispose();
                    resetEvent = null;
                }
			}

			if (image != null)
			{
				image.Dispose();
				image = null;
			}

			mediaEvent = null;

			hasVideo = false;
			hasAudio = false;

			basicAudio = null;
			basicVideo = null;
			mediaPosition = null;
			mediaSeeking = null;
			mediaEvent = null;
			mediaControl = null;
			vmrControl = null;
			vmrConfig = null;
			vmr = null;

			if (graphBuilder != null)
			{
				graphBuilder.Abort();
				graphBuilder = null;
			}

			if (filterGraph != null)
			{
				Marshal.ReleaseComObject(filterGraph);
				filterGraph = null;
			}

			state = MediaState.Unloaded;
		}

		void PollEvents ()
		{
			while (true)
			{
				if (resetEvent == null)
				{
					break;
				}

				if (resetEvent.WaitOne(100, true))
				{
					int hr;

					if (waitingForEventThreadToDie)
					{
						break;
					}

					do
					{
						if (mediaEvent == null)
						{
							break;
						}

						EventCode eventCode;
						IntPtr p1, p2;
						hr = mediaEvent.GetEvent(out eventCode, out p1, out p2, 0);

						if (hr >= 0)
						{
							if (eventCode == EventCode.Complete)
							{
								Invoke(new MethodInvoker (OnPlaybackFinished));
							}

							DsError.ThrowExceptionForHR(mediaEvent.FreeEventParams(eventCode, p1, p2));
						}
					}
					while (hr >= 0);
				}
			}

			waitingForEventThreadToDie = false;
		}

		public override void Play ()
		{
			if (state == MediaState.Unloaded)
			{
				throw new FileNotLoadedException ();
			}

			if (state != MediaState.Playing)
			{
				if (mediaControl != null)
				{
					DsError.ThrowExceptionForHR(mediaControl.Run());
					state = MediaState.Playing;

					if (HasAudio)
					{
						Volume = Volume; // This applies/removes muting. 
					}
				}
			}
		}

		public override void Rewind ()
		{
			if (state == MediaState.Unloaded)
			{
				throw new FileNotLoadedException ();
			}

            Pause();

			if (mediaSeeking != null)
			{
				DsError.ThrowExceptionForHR(mediaSeeking.SetPositions(new DsLong (0),
				                                                      AMSeekingSeekingFlags.AbsolutePositioning,
				                                                      null,
				                                                      AMSeekingSeekingFlags.NoPositioning));
			}
		}

		public override void Pause ()
		{
			if (mediaControl != null)
			{
				DsError.ThrowExceptionForHR(mediaControl.Pause());
				state = MediaState.Paused;
			}
		}

		public override void Stop ()
		{
			if (mediaControl != null)
			{
				DsError.ThrowExceptionForHR(mediaControl.Stop());
				UnloadMedia();
			}
		}

		public override bool Paused
		{
			get => (state == MediaState.Paused);

			set
			{
				if (value)
				{
					Pause();
				}
				else
				{
					Play();
				}
			}
		}

		public override bool IsPlaying => (state == MediaState.Playing);

		public override bool PlayLooped
		{
			get => playLooped;

			set => playLooped = value;
		}

		public override bool HasVideo => hasVideo;

		public override bool HasAudio => hasAudio;

		public override double Duration
		{
			get
			{
				if (state == MediaState.Unloaded)
				{
					throw new FileNotLoadedException ();
				}

				double length = 0;

				if (mediaPosition != null)
				{
					DsError.ThrowExceptionForHR(mediaPosition.get_Duration(out length));
				}
				return length;
			}
		}

		public override void Seek (double position)
		{
			if (state == MediaState.Unloaded)
			{
				throw new FileNotLoadedException ();
			}

			if (mediaPosition != null)
			{
				DsError.ThrowExceptionForHR(mediaPosition.put_CurrentPosition(position));
			}
		}

		public override double CurrentPosition
		{
			get
			{
				if (state == MediaState.Unloaded)
				{
					throw new FileNotLoadedException ();
				}

				double position = 0;

				if (mediaPosition != null)
				{
					DsError.ThrowExceptionForHR(mediaPosition.get_CurrentPosition(out position));
				}
				return position;
			}

			set => Seek(value);
		}

		public override double Speed
		{
			get
			{
				if (state == MediaState.Unloaded)
				{
                    return 1;
				}

				double speed = 0;

				if (mediaPosition != null)
				{
					DsError.ThrowExceptionForHR(mediaPosition.get_Rate(out speed));
				}
				return speed;
			}

            set
            {
                if (state != MediaState.Unloaded)
                {
                    if (hasAudio
						&& (basicAudio != null))
                    {
                        bool speedInRangeForAudioPlayback = ((value >= 0.5) && (value <= 2));

                        if (speedInRangeForAudioPlayback)
                        {
                            if (audioSavedState != null)
                            {
                                audioSavedState.Restore(state == MediaState.Playing);
                                audioSavedState.Dispose();
                                audioSavedState = null;
                            }
                        }
                        else
                        {
                            if (audioSavedState == null)
                            {
                                audioSavedState = new AudioFilterSavedState (filterGraph, mediaControl, (state == MediaState.Playing));
                            }
                        }
                    }

					if (mediaPosition != null)
					{
						int hResult = mediaPosition.put_Rate(value);
						if (throwOnSpeedErrors)
						{
							DsError.ThrowExceptionForHR(hResult);
						}
					}
                }
            }
		}

		static double LinearScaleFromMillibels (int millibels)
		{
			return Math.Pow(10, (millibels / 1000.0));
		}

		static int MillibelsFromLinearScale (double linear)		
		{
			return (int) Math.Max(-10000, Math.Min(0, (1000 * Math.Log10(Math.Max(1.0e-6, linear)))));
		}

		public override double Volume
		{
			get
			{
				if (state == MediaState.Unloaded)
				{
					throw new FileNotLoadedException ();
				}

				int volume = 1;

				if (hasAudio
					&& (basicAudio != null))
				{
					DsError.ThrowExceptionForHR(basicAudio.get_Volume(out volume));
				}
				return LinearScaleFromMillibels(volume);
			}

			set
			{
				if (state == MediaState.Unloaded)
				{
					throw new FileNotLoadedException ();
				}

				if (hasAudio
					&& (basicAudio != null))
				{
					DsError.ThrowExceptionForHR(basicAudio.put_Volume(Muted ? 0 : MillibelsFromLinearScale(value)));
				}
			}
		}

		public override string Filename
		{
			get => filename;

			set => LoadMedia(value);
		}

		public override Image TakeSnapshot ()
		{
			if (state == MediaState.Unloaded)
			{
				throw new FileNotLoadedException ();
			}

			if (image != null)
			{
				return new Bitmap (image);
			}

			if (! hasVideo)
			{
				throw new NoVideoException ();
			}

			IntPtr buffer = IntPtr.Zero;
			try
			{
				DsError.ThrowExceptionForHR(vmrControl.GetCurrentImage(out buffer));

				using (Image sourceImage = Conversion.GetBitmapFromDib(buffer))
				{
					Image destinationImage = new Bitmap (sourceImage.Width, sourceImage.Height);

					using (Graphics graphics = Graphics.FromImage(destinationImage))
					{
						graphics.DrawImage(sourceImage, 0, 0, destinationImage.Width, destinationImage.Height);
					}

					return destinationImage;
				}
			}
			finally
			{
				Marshal.FreeCoTaskMem(buffer);
			}
		}

		public override MediaState State => state;

		public override Size VideoSize
		{
			get
			{
				if (image != null)
				{
					return image.Size;
				}

				if (state == MediaState.Unloaded)
				{
					throw new FileNotLoadedException ();
				}

				if (image != null)
				{
					return image.Size;
				}

				if (! hasVideo)
				{
					throw new NoVideoException ();
				}

				return new Size (videoHeader.BmiHeader.Width, videoHeader.BmiHeader.Height);
			}
		}

		public override double AverageFrameRate
		{
			get
			{
				if (state == MediaState.Unloaded)
				{
					throw new FileNotLoadedException ();
				}

				if (! hasVideo)
				{
					throw new NoVideoException ();
				}

				double averageTimePerFrame;

				if (basicVideo.get_AvgTimePerFrame(out averageTimePerFrame) != 0)
				{
					throw new MediaPanelException ("Error calling get_AvgTimePerFrame()");
				}

				if (averageTimePerFrame < 0.001)
				{
					return 0;
				}

				return 1.0 / averageTimePerFrame;
			}
		}

		void UpdateVideoCropping ()
		{
			if (hasVideo)
			{
				if (vmrControl != null)
				{
					DsRect sourceRectangle = null;
					DsRect destinationRectangle = null;

					if ((videoHeader.BmiHeader.Width != 0)
					    && (videoHeader.BmiHeader.Height != 0)
					    && (Width != 0)
					    && (Height != 0))
					{
						Rectangle videoRectangle = Rectangle.Empty;
						Rectangle windowRectangle = Rectangle.Empty;

						switch (zoomMode)
						{
							case ZoomMode.PreserveAspectRatioWithCropping:
							{
								var xScale = Width / (float) videoHeader.BmiHeader.Width;
								var yScale = Height / (float) videoHeader.BmiHeader.Height;
								var scale = Math.Max(xScale, yScale);

								videoRectangle =
									new Rectangle(
										(int) ((videoReferencePoint.X * videoHeader.BmiHeader.Width) - (windowReferencePoint.X * Width / scale)),
										(int) ((videoReferencePoint.Y * videoHeader.BmiHeader.Height) - (windowReferencePoint.Y * Height / scale)),
										(int) (Width / scale), (int) (Height / scale));
								windowRectangle = ClientRectangle;
								break;
							}

							case ZoomMode.PreserveAspectRatioWithLetterboxing:
								videoRectangle = new Rectangle(0, 0, videoHeader.BmiHeader.Width, videoHeader.BmiHeader.Height);
								windowRectangle = ClientRectangle;
								break;
						}

						sourceRectangle = DsRect.FromRectangle(videoRectangle);
						destinationRectangle = DsRect.FromRectangle(windowRectangle);
					}

					DsError.ThrowExceptionForHR(vmrControl.SetVideoPosition(sourceRectangle, destinationRectangle));
				}
			}
		}

		void DoSize ()
		{
			UpdateVideoCropping();

			if (transparentPanel != null)
			{
				transparentPanel.Location = new Point (0, 0);
				transparentPanel.Size = Size;
			}
		}

		protected override void OnPlaybackFinished ()
		{
			base.OnPlaybackFinished();

			if (playLooped)
			{
				Rewind();
				Play();
			}
		}

		void transparentPanel_Click (object sender, EventArgs e)
		{
			OnClick(e);
		}

		void transparentPanel_DoubleClick (object sender, EventArgs e)
		{
			OnDoubleClick(e);
		}

		void transparentPanel_DragDrop (object sender, DragEventArgs e)
		{
			OnDragDrop(e);
		}

		void transparentPanel_DragEnter (object sender, DragEventArgs e)
		{
			OnDragEnter(e);
		}

		void transparentPanel_DragLeave (object sender, EventArgs e)
		{
			OnDragLeave(e);
		}

		void transparentPanel_DragOver (object sender, DragEventArgs e)
		{
			OnDragOver(e);
		}

		void transparentPanel_KeyDown (object sender, KeyEventArgs e)
		{
			OnKeyDown(e);
		}

		void transparentPanel_KeyPress (object sender, KeyPressEventArgs e)
		{
			OnKeyPress(e);
		}

		void transparentPanel_KeyUp (object sender, KeyEventArgs e)
		{
			OnKeyUp(e);
		}

		void transparentPanel_MouseClick (object sender, MouseEventArgs e)
		{
			OnMouseClick(e);
		}

		void transparentPanel_MouseDoubleClick (object sender, MouseEventArgs e)
		{
			OnMouseDoubleClick(e);
		}

		void transparentPanel_MouseDown (object sender, MouseEventArgs e)
		{
			OnMouseDown(e);
		}

		void transparentPanel_MouseEnter (object sender, EventArgs e)
		{
			OnMouseEnter(e);
		}

		void transparentPanel_MouseHover (object sender, EventArgs e)
		{
			OnMouseHover(e);
		}

		void transparentPanel_MouseLeave (object sender, EventArgs e)
		{
			OnMouseLeave(e);
		}

		void transparentPanel_MouseMove (object sender, MouseEventArgs e)
		{
			OnMouseMove(e);
		}

		void transparentPanel_MouseUp (object sender, MouseEventArgs e)
		{
			OnMouseUp(e);
		}

		void transparentPanel_MouseWheel (object sender, MouseEventArgs e)
		{
			OnMouseWheel(e);
		}

		public override bool ThrowOnSpeedErrors
		{
			get => throwOnSpeedErrors;

			set => throwOnSpeedErrors = value;
		}

		ZoomMode zoomMode;
		PointF videoReferencePoint;
		PointF windowReferencePoint;

		public override ZoomMode ZoomMode => zoomMode;

		public override void ZoomWithLetterboxing ()
		{
			zoomMode = ZoomMode.PreserveAspectRatioWithLetterboxing;

			UpdateVideoCropping();
		}

		public override void ZoomWithCropping (PointF windowReferencePoint, PointF videoReferencePoint)
		{
			zoomMode = ZoomMode.PreserveAspectRatioWithCropping;
			this.windowReferencePoint = windowReferencePoint;
			this.videoReferencePoint = videoReferencePoint;

			UpdateVideoCropping();
		}

		void SystemEvents_DisplaySettingsChanged (object sender, EventArgs e)
		{
			if (vmrControl != null)
			{
				vmrControl.DisplayModeChanged();
			}
		}

		protected override void OnMove (EventArgs e)
		{
			base.OnMove(e);

			DoSize();
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);

			DoSize();
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);

			if (image != null)
			{
				double xScale = ClientSize.Width * 1.0 / image.Width;
				double yScale = ClientSize.Height * 1.0 / image.Height;
				double scale = Math.Min(xScale, yScale);

				e.Graphics.DrawImage(image,
									 (float) ((ClientSize.Width - (image.Width * scale)) / 2),
									 (float) ((ClientSize.Height - (image.Height * scale)) / 2),
									 (float) (image.Width * scale),
									 (float) (image.Height * scale));
			}

			if (vmrControl != null)
			{
				IntPtr dc = IntPtr.Zero;
				try
				{
					dc = e.Graphics.GetHdc();
					DsError.ThrowExceptionForHR(vmrControl.SetVideoClippingWindow(Handle));
				}
				finally
				{
					e.Graphics.ReleaseHdc(dc);
				}
			}
		}
	}
}