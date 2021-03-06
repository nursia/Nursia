﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ModelViewer.UI;
using Myra;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.File;
using Nursia;
using Nursia.Graphics3D;
using Nursia.Graphics3D.ForwardRendering;
using Nursia.Graphics3D.Lights;
using Nursia.Graphics3D.Modelling;
using Nursia.Graphics3D.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace ModelViewer
{
	public class ViewerGame : Game
	{
		private readonly GraphicsDeviceManager _graphics;
		private NursiaModel _model;
		private CameraInputController _controller;
		private readonly ForwardRenderer _renderer = new ForwardRenderer();
		private MainPanel _mainPanel;
		private readonly FramesPerSecondCounter _fpsCounter = new FramesPerSecondCounter();
		private static readonly List<DirectLight> _defaultLights = new List<DirectLight>();
		private readonly Scene _scene = new Scene();

		static ViewerGame()
		{
			_defaultLights.Add(new DirectLight
			{
				Direction = new Vector3(-0.5265408f, -0.5735765f, -0.6275069f),
				Color = new Color(1, 0.9607844f, 0.8078432f)
			});

			_defaultLights.Add(new DirectLight
			{
				Direction = new Vector3(0.7198464f, 0.3420201f, 0.6040227f),
				Color = new Color(0.9647059f, 0.7607844f, 0.4078432f)
			});

			_defaultLights.Add(new DirectLight
			{
				Direction = new Vector3(0.4545195f, -0.7660444f, 0.4545195f),
				Color = new Color(0.3231373f, 0.3607844f, 0.3937255f)
			});
		}

		public ViewerGame()
		{
			_graphics = new GraphicsDeviceManager(this)
			{
				PreferredBackBufferWidth = 1200,
				PreferredBackBufferHeight = 800
			};

			Window.AllowUserResizing = true;
			IsMouseVisible = true;

			if (Configuration.NoFixedStep)
			{
				IsFixedTimeStep = false;
				_graphics.SynchronizeWithVerticalRetrace = false;
			}
		}

		private void LoadModel(string file)
		{
			if (!string.IsNullOrEmpty(file))
			{
				var folder = Path.GetDirectoryName(file);
				var data = File.ReadAllText(file);
				_model = NursiaModel.LoadFromJson(data,
					n =>
					{
						using (var stream = File.OpenRead(Path.Combine(folder, n)))
						{
							return Texture2D.FromStream(GraphicsDevice, stream);
						}
					});

				_mainPanel._comboAnimations.Items.Clear();
				_mainPanel._comboAnimations.Items.Add(new ListItem(null));
				foreach (var pair in _model.Animations)
				{
					_mainPanel._comboAnimations.Items.Add(
						new ListItem(pair.Key)
						{
							Tag = pair.Value
						});
				}

				_scene.Models.Clear();
				_scene.Models.Add(_model);
			}

			// Reset camera
			_scene.Camera.SetLookAt(new Vector3(10, 10, 10), Vector3.Zero);
		}

		protected override void LoadContent()
		{
			base.LoadContent();

			// UI
			MyraEnvironment.Game = this;
			_mainPanel = new MainPanel();
			_mainPanel._comboAnimations.Items.Clear();
			_mainPanel._comboAnimations.SelectedIndexChanged += _comboAnimations_SelectedIndexChanged;

			_mainPanel._buttonChange.Click += OnChangeFolder;

			_mainPanel._listFiles.SelectedIndexChanged += _listFiles_SelectedIndexChanged;

			_mainPanel._checkLightning.PressedChanged += _checkLightning_PressedChanged;

			Desktop.Root = _mainPanel;

			// Nursia
			Nrs.Game = this;
			LoadModel(string.Empty);

			var folder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
			folder = @"C:\Projects\Nursia\samples\models";
			SetFolder(folder);

			_controller = new CameraInputController(_scene.Camera);
		}

		private void _checkLightning_PressedChanged(object sender, EventArgs e)
		{
			_scene.Lights.Clear();
			if (_mainPanel._checkLightning.IsPressed)
			{
				_scene.Lights.AddRange(_defaultLights);
			}
		}

		private void _listFiles_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (_mainPanel._listFiles.SelectedItem == null)
			{
				LoadModel(null);
			} else
			{
				LoadModel(_mainPanel._listFiles.SelectedItem.Id);
			}
		}

		private void SetFolder(string folder)
		{
			_mainPanel._listFiles.Items.Clear();
			var files = Directory.EnumerateFiles(folder, "*.g3dj");
			foreach (var f in files)
			{
				var fileInfo = new FileInfo(f);
				if (fileInfo.Attributes.HasFlag(FileAttributes.Hidden))
				{
					continue;
				}

				_mainPanel._listFiles.Items.Add(new ListItem(fileInfo.Name)
				{
					Id = fileInfo.FullName
				});
			}

			_mainPanel._textPath.Text = folder;
		}

		private void OnChangeFolder(object sender, EventArgs e)
		{
;			var dlg = new FileDialog(FileDialogMode.ChooseFolder);

			try
			{
				if (!string.IsNullOrEmpty(_mainPanel._textPath.Text))
				{
					dlg.Folder = _mainPanel._textPath.Text;
				} else
				{
					var folder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
					dlg.Folder = folder;
				}
			}
			catch (Exception)
			{
			}

			dlg.Closed += (s, a) =>
			{
				if (!dlg.Result)
				{
					return;
				}

				SetFolder(dlg.FilePath);
			};

			dlg.ShowModal();
		}

		private void _comboAnimations_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if (_mainPanel._comboAnimations.SelectedItem == null)
			{
				_model.CurrentAnimation = null;
			}
			else
			{
				_model.CurrentAnimation = (ModelAnimation)_mainPanel._comboAnimations.SelectedItem.Tag;
			}
		}

		protected override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			_fpsCounter.Update(gameTime);

			var keyboardState = Keyboard.GetState();

			// Manage camera input controller
			_controller.SetControlKeyState(CameraInputController.ControlKeys.Left, keyboardState.IsKeyDown(Keys.A));
			_controller.SetControlKeyState(CameraInputController.ControlKeys.Right, keyboardState.IsKeyDown(Keys.D));
			_controller.SetControlKeyState(CameraInputController.ControlKeys.Forward, keyboardState.IsKeyDown(Keys.W));
			_controller.SetControlKeyState(CameraInputController.ControlKeys.Backward, keyboardState.IsKeyDown(Keys.S));
			_controller.SetControlKeyState(CameraInputController.ControlKeys.Up, keyboardState.IsKeyDown(Keys.Up));
			_controller.SetControlKeyState(CameraInputController.ControlKeys.Down, keyboardState.IsKeyDown(Keys.Down));

			var mouseState = Mouse.GetState();
			_controller.SetTouchState(CameraInputController.TouchType.Move, mouseState.LeftButton == ButtonState.Pressed);
			_controller.SetTouchState(CameraInputController.TouchType.Rotate, mouseState.RightButton == ButtonState.Pressed);

			_controller.SetMousePosition(new Point(mouseState.X, mouseState.Y));

			_controller.Update();
		}

		private void DrawModel()
		{
			if (_model == null)
			{
				return;
			}

			_model.UpdateCurrentAnimation();

			_renderer.Begin();
			_renderer.DrawScene(_scene);
			_renderer.End();
		}

		protected override void Draw(GameTime gameTime)
		{
			base.Draw(gameTime);

			GraphicsDevice.Clear(Color.Black);

			DrawModel();

			_mainPanel._labelCamera.Text = "Camera: " + _scene.Camera.ToString();
			_mainPanel._labelFps.Text = "FPS: " + _fpsCounter.FramesPerSecond;
			_mainPanel._labelMeshes.Text = "Meshes: " + _renderer.Statistics.MeshesDrawn;

			Desktop.Render();

			_fpsCounter.Draw(gameTime);
		}
	}
}