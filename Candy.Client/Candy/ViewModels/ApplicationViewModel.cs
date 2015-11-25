using System;
using System.IO;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Candy.Client.Messaging;
using Candy.Client.Models;
using Livet;
using Livet.Messaging;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Candy.Client.ViewModels
{
    /// <summary>
    /// アプリケーションに対応するビュー ロジックを提供します。
    /// </summary>
    public class ApplicationViewModel : ViewModel
    {
        private readonly InstalledApplication _app;

        public InstalledApplication Model
        {
            get { return _app; }
        }

        public ReactiveProperty<string> DisplayName { get; private set; }
        public ReactiveProperty<string> Definition { get; private set; }
        public ReactiveProperty<string> InstalledPath { get; private set; }
        public ReactiveProperty<bool> IsApplicationMissing { get; private set; }
        public ReactiveProperty<string> Version { get; private set; }
        public ReactiveProperty<BitmapSource> Image64 { get; private set; }
        public ReactiveProperty<BitmapSource> Image128 { get; private set; }
        public ReactiveProperty<UpdateSummary> Latest { get; private set; }
        public ReactiveProperty<bool> HasUpdate { get; set; }
        public ReactiveProperty<string> HasUpdateMessage { get; private set; }
        public ReactiveProperty<string> LatestVersion { get; private set; }
        public ReactiveProperty<string> ReleaseNote { get; set; }

        public ReactiveCommand ExecuteCommand { get; private set; }
        public ReactiveCommand UpdateCommand { get; private set; }
        public ReactiveCommand RemoveCommand { get; private set; }

        public ApplicationViewModel(ApplicationManager manager, InstalledApplication app)
        {
            _app = app;

            DisplayName = app.ObserveProperty(x => x.DisplayName).ToReactiveProperty();
            Definition = app.ObserveProperty(x => x.Definition).ToReactiveProperty();
            InstalledPath = app.ObserveProperty(x => x.InstalledPath).ToReactiveProperty();
            IsApplicationMissing = InstalledPath.Select(File.Exists).ToReactiveProperty();

            var currentVersion = app.ObserveProperty(x => x.ApplicationVersion).ToReactiveProperty();

            Version = currentVersion.Where(x => x != null)
                                    .Select(x => x.ToString())
                                    .ToReactiveProperty();

            Image64 = InstalledPath.Select(x => GetIcon(x, 64)).ToReactiveProperty();
            Image128 = InstalledPath.Select(x => GetIcon(x, 128)).ToReactiveProperty();

            Latest = app.ObserveProperty(x => x.Latest).ToReactiveProperty();

            HasUpdate = Latest.Select(x => x != null).ToReactiveProperty();
            HasUpdateMessage = HasUpdate.Select(x => x ? "最新版があります" : "お使いのバージョンは最新です").ToReactiveProperty();

            LatestVersion = Latest.CombineLatest(currentVersion, (latest, current) => latest != null ? latest.Version : current)
                                  .Where(x => x != null)
                                  .Select(x => x.ToString())
                                  .ToReactiveProperty();

            ReleaseNote = Latest.Select(x => x != null ? x.ReleaseNote : "更新情報はありません。")
                                .ToReactiveProperty();

            ExecuteCommand = new ReactiveCommand(app.ObserveProperty(x => x.CanExecute));
            ExecuteCommand.Subscribe(_ =>
            {
                app.ExecuteAsync();
            });

            UpdateCommand = new ReactiveCommand(Latest.Select(x => x != null));
            UpdateCommand.Subscribe(async _ =>
            {
                var res = await Messenger.GetResponse(new AsyncConfirmationMessage
                {
                    MessageKey = "Confirm",
                    Caption = "確認",
                    Text = app.UpdateConfirmationMessage
            
                }).Response;

                if (res != true) return;

                await app.UpdateAsync();

                Messenger.Raise(new AsyncInformationMessage
                {
                    MessageKey = "Close"
                });

                Messenger.Raise(new AsyncInformationMessage
                {
                    MessageKey = "Information",
                    Caption = "完了",
                    Text = "アップデートが完了しました。"
                });
            });

            RemoveCommand = new ReactiveCommand(app.ObserveProperty(x => x.CanRemove));
            RemoveCommand.Subscribe(async _ =>
            {
                var res = await Messenger.GetResponse(new AsyncConfirmationMessage
                {
                    MessageKey = "Confirm",
                    Caption = "確認",
                    Text = "アプリケーションの登録を削除してもよろしいですか？（アプリケーション自体は削除されません）"
            
                }).Response;

                if (res != true) return;

                await manager.RemoveApplicationAsync(app);

                Messenger.Raise(new AsyncInformationMessage
                {
                    MessageKey = "Close"
                });
            });
        }

        private static BitmapSource GetIcon(string filePath, int size)
        {
            if (!File.Exists(filePath)) return null;

            IntPtr hbitmap;
            IShellItem iShellItem;
            var iIdIShellItem = new Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe");
            SHCreateItemFromParsingName(filePath, IntPtr.Zero, iIdIShellItem, out iShellItem);
            ((IShellItemImageFactory)iShellItem).GetImage(new SIZE(size, size), 0x0, out hbitmap);
            
            var image = Imaging.CreateBitmapSourceFromHBitmap(hbitmap, IntPtr.Zero, Int32Rect.Empty, null);
            image.Freeze();
            return image;
        }
        #region Win32
        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
        public interface IShellItem
        {
            void BindToHandler(IntPtr pbc,
                [MarshalAs(UnmanagedType.LPStruct)]Guid bhid,
                [MarshalAs(UnmanagedType.LPStruct)]Guid riid,
                out IntPtr ppv);

            void GetParent(out IShellItem ppsi);

            void GetDisplayName(SIGDN sigdnName, out IntPtr ppszName);

            void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);

            void Compare(IShellItem psi, uint hint, out int piOrder);
        };
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void SHCreateItemFromParsingName(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            [In] IntPtr pbc,
            [In][MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [Out][MarshalAs(UnmanagedType.Interface, IidParameterIndex = 2)] out IShellItem ppv);

        [ComImport()]
        [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IShellItemImageFactory
        {
            void GetImage(
            [In, MarshalAs(UnmanagedType.Struct)] SIZE size,
            [In] SIIGBF flags,
            [Out] out IntPtr phbm);
        }

        public enum SIGDN : uint
        {
            NORMALDISPLAY = 0,
            PARENTRELATIVEPARSING = 0x80018001,
            PARENTRELATIVEFORADDRESSBAR = 0x8001c001,
            DESKTOPABSOLUTEPARSING = 0x80028000,
            PARENTRELATIVEEDITING = 0x80031001,
            DESKTOPABSOLUTEEDITING = 0x8004c000,
            FILESYSPATH = 0x80058000,
            URL = 0x80068000
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct SIZE
        {
            public int cx;
            public int cy;

            public SIZE(int cx, int cy)
            {
                this.cx = cx;
                this.cy = cy;
            }
        }
        [Flags]
        public enum SIIGBF
        {
            SIIGBF_RESIZETOFIT = 0x00,
            SIIGBF_BIGGERSIZEOK = 0x01,
            SIIGBF_MEMORYONLY = 0x02,
            SIIGBF_ICONONLY = 0x04,
            SIIGBF_THUMBNAILONLY = 0x08,
            SIIGBF_INCACHEONLY = 0x10,
        }
        #endregion
    }
}
