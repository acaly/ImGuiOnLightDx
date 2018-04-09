using ImGuiNET;
using LightDx;
using LightDx.InputAttributes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImGuiOnLightDx
{
    internal unsafe class SampleWindow
    {
        public struct Vertex
        {
            [Position]
            public Float2 Position;
            [TexCoord]
            public Float2 TexCoord;
            [Color]
            public uint Color;
        }

        public SampleWindow()
        {
            _textInputBufferLength = 1024;
            _textInputBuffer = Marshal.AllocHGlobal(_textInputBufferLength);
            long* ptr = (long*)_textInputBuffer.ToPointer();
            for (int i = 0; i < 1024 / sizeof(long); i++)
            {
                ptr[i] = 0;
            }
            SetKeyMappings();
        }

        private LightDevice _device;
        private Form _form;
        private InputDataProcessor<Vertex> _inputDataProcessor;
        private InputBuffer _vertexBuffer;
        private Vertex[] _vertexBufferData;
        private IndexBuffer _indexBuffer;
        private ushort[] _indexBufferData;

        private System.Numerics.Vector4 _buttonColor = new System.Numerics.Vector4(55f / 255f, 155f / 255f, 1f, 1f);
        private int _pressCount = 0;
        private float _sliderVal = 0;
        private System.Numerics.Vector3 _positionValue = new System.Numerics.Vector3(500);
        private IntPtr _textInputBuffer;
        private int _textInputBufferLength;

        private void UpdateVB(int num, void* ptr)
        {
            if (_vertexBufferData == null || _vertexBufferData.Length < num)
            {
                _vertexBufferData = new Vertex[num + 200];
                if (_vertexBuffer != null) _vertexBuffer.Dispose();
                _vertexBuffer = _inputDataProcessor.CreateDynamicBuffer(_vertexBufferData.Length);
            }
            fixed (Vertex* dest = _vertexBufferData)
            {
                Buffer.MemoryCopy(ptr, dest,
                    Marshal.SizeOf<Vertex>() * _vertexBufferData.Length,
                    Marshal.SizeOf<Vertex>() * num);
            }
            _inputDataProcessor.UpdateBufferDynamic(_vertexBuffer, _vertexBufferData);
        }

        private void UpdateIB(int num, ushort* data)
        {
            if (_indexBufferData == null || _indexBufferData.Length < num)
            {
                _indexBufferData = new ushort[num + 200];
                if (_indexBuffer != null) _indexBuffer.Dispose();
                _indexBuffer = _device.CreateDynamicIndexBuffer(16, _indexBufferData.Length);
            }
            fixed (ushort* dest = _indexBufferData)
            {
                Buffer.MemoryCopy(data, dest, 2 * _indexBufferData.Length, 2 * num);
            }
            _indexBuffer.UpdateDynamic(_indexBufferData);
        }

        private void UpdateImGuiInput(IO io)
        {
            if (_form.Focused)
            {
                Point windowPoint = _form.PointToClient(Control.MousePosition);
                io.MousePosition = new System.Numerics.Vector2(windowPoint.X / io.DisplayFramebufferScale.X, windowPoint.Y / io.DisplayFramebufferScale.Y);
            }
            else
            {
                io.MousePosition = new System.Numerics.Vector2(-1f, -1f);
            }

            io.MouseDown[0] = Control.MouseButtons.HasFlag(MouseButtons.Left);
            io.MouseDown[1] = Control.MouseButtons.HasFlag(MouseButtons.Right);
            io.MouseDown[2] = Control.MouseButtons.HasFlag(MouseButtons.Middle);

            io.MouseWheel = 0;
        }

        private Texture2D CreateFontTexture()
        {
            IO io = ImGui.GetIO();

            // Build texture atlas
            FontTextureData texData = io.FontAtlas.GetTexDataAsRGBA32();
            using (var bitmap = new Bitmap(texData.Width, texData.Height, PixelFormat.Format32bppArgb))
            {
                var srcStride = texData.BytesPerPixel * texData.Width;
                var locked = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                for (int i = 0; i < locked.Height; ++i)
                {
                    Buffer.MemoryCopy(
                        texData.Pixels + srcStride * i,
                        (locked.Scan0 + locked.Stride * i).ToPointer(),
                        locked.Width * 4, locked.Width * 4);
                }
                bitmap.UnlockBits(locked);
                var font = _device.CreateTexture2D(bitmap);

                io.FontAtlas.SetTexID(1);
                io.FontAtlas.ClearTexData();

                return font;
            }
        }

        private int OnTextEdited(TextEditCallbackData* data)
        {
            return 0;
        }

        private void SubmitImGuiStuff()
        {
            bool mainWindowOpened = true;

            ImGui.GetStyle().WindowRounding = 0;

            ImGui.SetNextWindowSize(new System.Numerics.Vector2(800, 600 - 20), Condition.Always);
            ImGui.SetNextWindowPos(ImGui.GetIO().DisplaySize, Condition.Always, new System.Numerics.Vector2(1f));
            ImGui.BeginWindow("ImGUI.NET Sample Program", ref mainWindowOpened, WindowFlags.NoResize | WindowFlags.NoTitleBar | WindowFlags.NoMove);

            ImGui.BeginMainMenuBar();
            if (ImGui.BeginMenu("Help"))
            {
                if (ImGui.MenuItem("About", "Ctrl-Alt-A", false, true))
                {

                }
                ImGui.EndMenu();
            }
            ImGui.EndMainMenuBar();

            ImGui.Text("Hello,");
            ImGui.Text("World!");
            ImGui.Text("From ImGui.NET. ...Did that work?");
            var pos = ImGui.GetIO().MousePosition;
            bool leftPressed = ImGui.GetIO().MouseDown[0];
            ImGui.Text("Current mouse position: " + pos + ". Pressed=" + leftPressed);

            ImGui.ShowStyleSelector("Select style");

            if (ImGui.Button("Increment the counter."))
            {
                _pressCount += 1;
            }

            ImGui.Text($"Button pressed {_pressCount} times.", new System.Numerics.Vector4(0, 1, 1, 1));

            ImGui.InputTextMultiline("Input some text:",
                _textInputBuffer, (uint)_textInputBufferLength,
                new System.Numerics.Vector2(360, 240),
                InputTextFlags.Default,
                OnTextEdited);

            ImGui.SliderFloat("SlidableValue", ref _sliderVal, -50f, 100f, $"{_sliderVal.ToString("##0.00")}", 1.0f);
            ImGui.DragVector3("Vector3", ref _positionValue, -100, 100);

            if (ImGui.TreeNode("First Item"))
            {
                ImGui.Text("Word!");
                ImGui.TreePop();
            }
            if (ImGui.TreeNode("Second Item"))
            {
                ImGui.ColorButton("Color button", _buttonColor, ColorEditFlags.Default, new System.Numerics.Vector2(0, 0));
                if (ImGui.Button("Push me to change color", new System.Numerics.Vector2(0, 30)))
                {
                    _buttonColor = new System.Numerics.Vector4(_buttonColor.Y + .25f, _buttonColor.Z, _buttonColor.X, _buttonColor.W);
                    if (_buttonColor.X > 1.0f)
                    {
                        _buttonColor.X -= 1.0f;
                    }
                }

                ImGui.TreePop();
            }

            if (ImGui.Button("Press me!", new System.Numerics.Vector2(100, 30)))
            {
                ImGuiNative.igOpenPopup("SmallButtonPopup");
            }

            if (ImGui.BeginPopup("SmallButtonPopup"))
            {
                ImGui.Text("Here's a popup menu.");
                ImGui.Text("With two lines.");

                ImGui.EndPopup();
            }

            if (ImGui.Button("Open Modal window"))
            {
                ImGui.OpenPopup("ModalPopup");
            }
            if (ImGui.BeginPopupModal("ModalPopup"))
            {
                ImGui.Text("You can't press on anything else right now.");
                ImGui.Text("You are stuck here.");
                if (ImGui.Button("OK", new System.Numerics.Vector2(0, 0))) { }
                ImGui.SameLine();
                ImGui.Dummy(100f, 0f);
                ImGui.SameLine();
                if (ImGui.Button("Please go away", new System.Numerics.Vector2(0, 0))) { ImGui.CloseCurrentPopup(); }

                ImGui.EndPopup();
            }

            ImGui.Text("I have a context menu.");
            if (ImGui.BeginPopupContextItem("ItemContextMenu"))
            {
                if (ImGui.Selectable("How's this for a great menu?")) { }
                ImGui.Selectable("Just click somewhere to get rid of me.");
                ImGui.EndPopup();
            }

            //Not yet supported in nuget
            //ImGui.Text("ProgressBar:");
            //ImGui.ProgressBar(0.5f, new System.Numerics.Vector2(300, 20), "50%");

            ImGui.EndWindow();
        }

        private void RenderImDrawData(DrawData* data)
        {
            for (int n = 0; n < data->CmdListsCount; n++)
            {
                NativeDrawList* cmdList = data->CmdLists[n];
                byte* vb = (byte*)cmdList->VtxBuffer.Data;
                ushort* ib = (ushort*)cmdList->IdxBuffer.Data;

                UpdateVB(cmdList->VtxBuffer.Size, vb);

                for (int cmd_i = 0; cmd_i < cmdList->CmdBuffer.Size; cmd_i++)
                {
                    DrawCmd* pcmd = &(((DrawCmd*)cmdList->CmdBuffer.Data)[cmd_i]);
                    if (pcmd->UserCallback != IntPtr.Zero)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        var texid = pcmd->TextureId.ToInt32();

                        UpdateIB((int)pcmd->ElemCount, ib);
                        _indexBuffer.Draw(_vertexBuffer, 0, (int)pcmd->ElemCount);
                    }
                    ib += pcmd->ElemCount;
                }
            }
        }

        private void RenderFrame()
        {
            IO io = ImGui.GetIO();
            io.DisplaySize = new System.Numerics.Vector2(800, 600);
            io.DisplayFramebufferScale = new System.Numerics.Vector2(1);
            io.DeltaTime = (1f / 60f);

            UpdateImGuiInput(io);

            ImGui.NewFrame();

            SubmitImGuiStuff();

            ImGui.Render();

            DrawData* data = ImGui.GetDrawData();
            RenderImDrawData(data);
        }

        private void SetKeyMappings()
        {
            IO io = ImGui.GetIO();
            io.KeyMap[GuiKey.Tab] = (int)Keys.Tab;
            io.KeyMap[GuiKey.LeftArrow] = (int)Keys.Left;
            io.KeyMap[GuiKey.RightArrow] = (int)Keys.Right;
            io.KeyMap[GuiKey.UpArrow] = (int)Keys.Up;
            io.KeyMap[GuiKey.DownArrow] = (int)Keys.Down;
            io.KeyMap[GuiKey.PageUp] = (int)Keys.PageUp;
            io.KeyMap[GuiKey.PageDown] = (int)Keys.PageDown;
            io.KeyMap[GuiKey.Home] = (int)Keys.Home;
            io.KeyMap[GuiKey.End] = (int)Keys.End;
            io.KeyMap[GuiKey.Delete] = (int)Keys.Delete;
            io.KeyMap[GuiKey.Backspace] = (int)Keys.Back;
            io.KeyMap[GuiKey.Enter] = (int)Keys.Enter;
            io.KeyMap[GuiKey.Escape] = (int)Keys.Escape;
            io.KeyMap[GuiKey.A] = (int)Keys.A;
            io.KeyMap[GuiKey.C] = (int)Keys.C;
            io.KeyMap[GuiKey.V] = (int)Keys.V;
            io.KeyMap[GuiKey.X] = (int)Keys.X;
            io.KeyMap[GuiKey.Y] = (int)Keys.Y;
            io.KeyMap[GuiKey.Z] = (int)Keys.Z;
        }

        private unsafe void OnKeyDown(object sender, KeyEventArgs e)
        {
            ImGui.GetIO().KeysDown[(int)e.KeyCode] = true;
            UpdateModifiers(e);
        }

        private unsafe void OnKeyUp(object sender, KeyEventArgs e)
        {
            ImGui.GetIO().KeysDown[(int)e.KeyCode] = false;
            UpdateModifiers(e);
        }

        private static unsafe void UpdateModifiers(KeyEventArgs e)
        {
            IO io = ImGui.GetIO();
            io.AltPressed = e.Alt;
            io.CtrlPressed = e.Control;
            io.ShiftPressed = e.Shift;
        }

        public void Run()
        {
            using (var form = new Form())
            {
                _form = form;
                form.Text = "ImGui.NET on LightDx";
                form.ClientSize = new Size(800, 600);
                form.FormBorderStyle = FormBorderStyle.Fixed3D;
                form.MaximizeBox = false;
                form.KeyDown += OnKeyDown;
                form.KeyUp += OnKeyUp;

                using (var device = LightDevice.Create(form))
                {
                    _device = device;

                    var target = device.CreateDefaultTarget(false);
                    target.Apply();

                    Pipeline pipeline;
                    using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ImGuiOnLightDx.Shader.fx"))
                    {
                        pipeline = device.CompilePipeline(ShaderSource.FromStream(stream), false, InputTopology.Triangle);
                    }
                    
                    pipeline.SetResource(0, CreateFontTexture());
                    pipeline.SetBlender(Blender.AlphaBlender);

                    pipeline.Apply();

                    var input = pipeline.CreateInputDataProcessor<Vertex>();
                    _inputDataProcessor = input;

                    form.Show();
                    device.RunLoop(delegate ()
                    {
                        target.ClearAll(Color.White);

                        RenderFrame();

                        device.Present(true);
                    });
                }
            }
        }
    }
}
