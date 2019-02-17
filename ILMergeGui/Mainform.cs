#region Header

/* ----------   ---   -------------------------------------------------------------------------------
 * Purpose:           Gui for Microsoft's ILMerge.
 * By:                G.W. van der Vegt (wim@vander-vegt.nl)
 * Based on:          VB version by igoramadas (devv.com) at http://ilmergegui.codeplex.com/
 * Depends:           Nothing (well except ILMerge.exe).
 * ----------   ---   -------------------------------------------------------------------------------
 * dd-mm-yyyy - who - description
 * ----------   ---   -------------------------------------------------------------------------------
 * 04-05-2012 - veg - Added Relative Paths to Assembly List (relative to primary).
 *                  - Added shortend Display values to Assembly List.
 *                  - Search for all Windows .Net Frameworks (x86, x64) in
 *                    C:\Windows\Microsoft.NET and
 *                    C:\Program Files\Reference Assemblies.
 *                    C:\Program Files (x86)\Reference Assemblies.
 *                  - Dynamically build Framework Combobox.
 *                  - Added Delete to Assembly Listbox.
 *                  - Added Drag&Drop to Assembly Listbox.
 *                  - Re-added checks on outputpath/assembly collisions.
 *                  - Added autocompletion to editoboxes.
 *                  - Disable merge button when only a single assembly is present.
 *                  - Show logfile (if present and generated) after merge.
 *                  - Debugged DynaInvoke.
 *                  - Renamed methods in DynaInvoke.
 *                  - Disabled Slow Method in DynaInvoke.
 *                  - ILMerge path changes according to x86 or x64 architecture.
 *                  - Improved error message of IlMerge.Merge() (using the inner exception).
 *                  - Added Diagnostic output for ILMerge() method and property access.
 *                  - Clear filename of OpenDialog.
 *                  - Changed linklabels
 *                  - Removed donate.
 *                  - Debugged MakeRelativePath (same paths returned empty string).
 * ----------   ---   -------------------------------------------------------------------------------
 * 05-05-2012 - veg - Added menubar.
 *                  - Added saving and restoring of settings in xml format.
 * ----------   ---   -------------------------------------------------------------------------------
 * 21-06-2012 - veg - Improved ILMerge.exe searching (added path and assembly registry).
 *                  - Swapped ListBox for ListView.
 *                  - Added transparent background watermark image.
 *                  - Changed logic around formatting the display values.
 *                    Reformat them all using the ListViewItem Tag property as storage for original filenames.
 *                  - Change code to use Tag property for filename (TEST!).
 * ----------   ---   -------------------------------------------------------------------------------
 * 22-06-2012 - veg - Added support for dropping (sub) directories.
 *                  - Added checkboxes for primary assembly instead of selected item.
 *                  - Refactored code a bit.
 *                  - Added version column.
 *                  - Enabled AutoVersionIncrement on Rebuild.
 * ----------   ---   -------------------------------------------------------------------------------
 * 19-12-2012 - veg - Added Internalize support.
 *                  - Added support for merging XmlDocumentation.
 *                  - Added version number to main form (should be 2.0.4 for this release).
 *                  - Updated click-once installer.
 *                  - Fixed merging assemblies into a dll.
 *                  - Released as v2.0.4
 * ----------   ---   -------------------------------------------------------------------------------
 * 19-12-2012 - veg - Fixed workitem 8741.
 * 06-11-2013 - veg - Hopefully fixed workitem 8745.
 *                    .Net v4.5 fixup. 4.5 is an inplace upgrade of 4.0 which does not alter the version number.
 *                    See http://www.mattwrock.com/post/2012/02/29/What-you-should-know-about-running-ILMerge-on-Net-45-Beta-assemblies-targeting-Net-40.aspx
 * ----------   ---   -------------------------------------------------------------------------------
 * 10-11-2013 - veg - Started on ILRepack Support.
 *                  - First implementation works
 *                    1) No Support for v2.0, v3.x and v4.5.
 *                    2) Not all features are tested yet.
 *                    3) Engine not saved to configuration (*.ilproj) file.
 *                  - Made engine switchable.
 *                  - Engine saved to/restored from configuration (*.ilproj) file.
 * ----------   ---   -------------------------------------------------------------------------------
 * 16-11-2013 - veg - Removed deprecated .Net Keys (v4.0 Client).
 *                  - Fixed DotNet key values.
 * ----------   ---   -------------------------------------------------------------------------------
 *                    TODO MRU on MainMenu (.ilproj) seems to fail!
 *                    TODO Settings SetTargetPlatform on ILRepack leads to stack overflow.
 *                    TODO ILRepack cannot merge twice (does either not delete the output or SetInputAssemblies files are not cleared properly).
 *                    TODO Generate ILMerge.exe Commandline.
 *                    TODO Generate ILRepack.exe Commandline.
 * ----------   ---   -------------------------------------------------------------------------------
 * 27-12-2012 - veg - Added new switched (internalize and mergexml) to ilproj settings.
 *                  - Added registration project file type (once, if running elevated).
 *                  - Added indication that project file type is registred or not.
 * ----------   ---   -------------------------------------------------------------------------------
 * 03-09-2014 - veg - Added automatic creation of output directory.
 *                  - Added generated command-line examples.
 *                  - BUG ILRepack seems to cache input assemblies and will not execute a second time.
 *                  - Added default<T> return to GetProperty if invoked value was null.
 * 
 *                  - Checked supported frameworks:
 *                  
 *                    		            ilmerge	ilrepack
 *                    		            ------- --------
 *                    v2.050727 sp2 	ok	    ok
 *                    v3.0 sp2	        no	    ok
 *                    v3.5 sp1	        no	    ok
 *                    v4.5 client	    ok	    ok
 *                    v4.5 full	        ok	    ok
 *                    
 *                    Version 2.0.7.0.
 * ----------   ---   -------------------------------------------------------------------------------
 */

#endregion Header

namespace ILMergeGui
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Windows.Forms;
    using System.Xml;
    using System.Xml.Linq;
    using ILMergeGui.Properties;
    using Microsoft.Win32;
    using Swiss;

    //! TODO Debug ILMerge call (Tag property).
    //! TODO Restore Groups on Xml Restore.
    //! TODO Watermark won't remove.
    // TODO Make sure dialogs are cleaned before re-use.
    // TODO Add commandline options (cfg & /Merge).
    // TODO Find out what ILMerge commandline options actually mean.
    // TODO Detect CF Framework (C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework)
    // TODO Detect MicroFramework (MicroFrameworkPK_v4_1).
    //       HKEY_CURRENT_USER\Software\Microsoft\VisualStudio\10.0_Config\MSBuild\SafeImports
    //       HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\Folders (scan for keys with \ReferenceAssembly\Micrososft\Framework)
    //       HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S-1-5-18\Components\14A3CECB2D8CDD549B5B500B9419DD8B   06CB6B5FEBFB8C64592234F1A39D5C3E
    //       HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S-1-5-18\Components\20D9905CA04002C46AA268E18E602954   C19BEE65E0D80E340B3348E8D3F2A593 (Arm)
    //       HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S-1-5-18\Components\33FEA56D2D876AA409FE8D29504C4941   325D13E28C59BD44097CE0F472F4EC95 (Thumb)
    //       HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S-1-5-18\Components\4F5BF0F9A63E3F34C97EA3E33BB60492   4D0ABD3C1BE9C944C85FFCA0F1F6F7A4 (Porting kit)
    //
    //       x86 = C:\Windows\Microsoft.NET\Framework
    //       x64 = C:\Windows\Microsoft.NET\Framework64
    // TODO Windows Communication Foundation
    // TODO Windows Presentation Foundation
    // TODO Windows Workflow Foundation
    // TODO Advanced Settings?
    // TODO List special features under v3.0 as Windows Workflow Foundationn.
    //! TODO Support for Version 1/1.1
    //! TODO http://support.microsoft.com/kb/318785/en-us
    //! Reflected methods from ilMerge.exe assembly:
    //public bool AllowMultipleAssemblyLevelAttributes { set; get; }
    //public bool AllowWildCards { set; get; }
    //public bool AllowZeroPeKind { set; get; }
    //public string AttributeFile { set; get; }
    //public bool Closed { set; get; }
    //public string ExcludeFile { set; get; }
    //public int FileAlignment { set; get; }
    //public bool Internalize { set; get; }
    //public bool PreserveShortBranches { set; get; }
    //public bool PublicKeyTokens { set; get; }
    //public bool StrongNameLost { get; }
    //public System.Version Version { set; get; }
    //public bool XmlDocumentation { set; get; }
    //public void AllowDuplicateType(string typeName)
    //public void SetSearchDirectories(string[] dirs)
    public partial class Mainform : Form
    {
        #region Fields

        /// <summary>
        /// Used for ListView.
        /// </summary>
        private const uint CLR_NONE = 0xFFFFFFFF;
        const String MyAppName = "ILMergeGui";
        const String MyExtension = ".ilproj";
        const String MyWildcard = "*" + MyExtension;

        private DotNet framework;
        private String frameversion;
        private List<String> arrAssemblies;

        /// <summary>
        /// Storage for Available DotNet Frameworks.
        /// </summary>
        private List<DotNet> frameworks = null;

        //MRU Code
        private MruStripMenu mruMenu;
        private string mruRegKey = "SOFTWARE\\" + Application.CompanyName + "\\ " + AppTitle;
        private bool AutoClose;
        private int ExitCode;
        private string ExitMsg;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        public Mainform()
        {
            InitializeComponent();
        }

        #endregion Constructors

        #region Enumerations

        /// <summary>
        /// http://social.msdn.microsoft.com/forums/en-US/winforms/thread/86d8a8bf-8bc0-4567-970b-19a96b0e9b7c/
        /// </summary>
        [Flags]
        internal enum LVBKIF
        {
            SOURCE_NONE = 0x00000000,
            SOURCE_HBITMAP = 0x00000001,
            SOURCE_URL = 0x00000002,
            SOURCE_MASK = 0x00000003,
            STYLE_NORMAL = 0x00000000,
            STYLE_TILE = 0x00000010,
            STYLE_MASK = 0x00000010,
            FLAG_TILEOFFSET = 0x00000100,
            TYPE_WATERMARK = 0x10000000,
            FLAG_ALPHABLEND = 0x20000000
        }

        /// <summary>
        /// http://social.msdn.microsoft.com/forums/en-US/winforms/thread/86d8a8bf-8bc0-4567-970b-19a96b0e9b7c/
        /// </summary>
        // Enumeration is set to unicode, ANSI counterparts are commented out.
        // Contains a few undocumented messages of which the name was invented.
        internal enum LVM
        {
            FIRST = 0x1000,
            SETUNICODEFORMAT = 0x2005,        // CCM_SETUNICODEFORMAT,
            GETUNICODEFORMAT = 0x2006,        // CCM_GETUNICODEFORMAT,
            GETBKCOLOR = (FIRST + 0),
            SETBKCOLOR = (FIRST + 1),
            GETIMAGELIST = (FIRST + 2),
            SETIMAGELIST = (FIRST + 3),
            GETITEMCOUNT = (FIRST + 4),
            GETITEMA = (FIRST + 5),
            GETITEMW = (FIRST + 75),
            GETITEM = GETITEMW,
            //GETITEM                = GETITEMA,
            SETITEMA = (FIRST + 6),
            SETITEMW = (FIRST + 76),
            SETITEM = SETITEMW,
            //SETITEM                = SETITEMA,
            INSERTITEMA = (FIRST + 7),
            INSERTITEMW = (FIRST + 77),
            INSERTITEM = INSERTITEMW,
            //INSERTITEM             = INSERTITEMA,
            DELETEITEM = (FIRST + 8),
            DELETEALLITEMS = (FIRST + 9),
            GETCALLBACKMASK = (FIRST + 10),
            SETCALLBACKMASK = (FIRST + 11),
            GETNEXTITEM = (FIRST + 12),
            FINDITEMA = (FIRST + 13),
            FINDITEMW = (FIRST + 83),
            GETITEMRECT = (FIRST + 14),
            SETITEMPOSITION = (FIRST + 15),
            GETITEMPOSITION = (FIRST + 16),
            GETSTRINGWIDTHA = (FIRST + 17),
            GETSTRINGWIDTHW = (FIRST + 87),
            HITTEST = (FIRST + 18),
            ENSUREVISIBLE = (FIRST + 19),
            SCROLL = (FIRST + 20),
            REDRAWITEMS = (FIRST + 21),
            ARRANGE = (FIRST + 22),
            EDITLABELA = (FIRST + 23),
            EDITLABELW = (FIRST + 118),
            EDITLABEL = EDITLABELW,
            //EDITLABEL              = EDITLABELA,
            GETEDITCONTROL = (FIRST + 24),
            GETCOLUMNA = (FIRST + 25),
            GETCOLUMNW = (FIRST + 95),
            SETCOLUMNA = (FIRST + 26),
            SETCOLUMNW = (FIRST + 96),
            INSERTCOLUMNA = (FIRST + 27),
            INSERTCOLUMNW = (FIRST + 97),
            DELETECOLUMN = (FIRST + 28),
            GETCOLUMNWIDTH = (FIRST + 29),
            SETCOLUMNWIDTH = (FIRST + 30),
            GETHEADER = (FIRST + 31),
            CREATEDRAGIMAGE = (FIRST + 33),
            GETVIEWRECT = (FIRST + 34),
            GETTEXTCOLOR = (FIRST + 35),
            SETTEXTCOLOR = (FIRST + 36),
            GETTEXTBKCOLOR = (FIRST + 37),
            SETTEXTBKCOLOR = (FIRST + 38),
            GETTOPINDEX = (FIRST + 39),
            GETCOUNTPERPAGE = (FIRST + 40),
            GETORIGIN = (FIRST + 41),
            UPDATE = (FIRST + 42),
            SETITEMSTATE = (FIRST + 43),
            GETITEMSTATE = (FIRST + 44),
            GETITEMTEXTA = (FIRST + 45),
            GETITEMTEXTW = (FIRST + 115),
            SETITEMTEXTA = (FIRST + 46),
            SETITEMTEXTW = (FIRST + 116),
            SETITEMCOUNT = (FIRST + 47),
            SORTITEMS = (FIRST + 48),
            SETITEMPOSITION32 = (FIRST + 49),
            GETSELECTEDCOUNT = (FIRST + 50),
            GETITEMSPACING = (FIRST + 51),
            GETISEARCHSTRINGA = (FIRST + 52),
            GETISEARCHSTRINGW = (FIRST + 117),
            GETISEARCHSTRING = GETISEARCHSTRINGW,
            //GETISEARCHSTRING       = GETISEARCHSTRINGA,
            SETICONSPACING = (FIRST + 53),
            SETEXTENDEDLISTVIEWSTYLE = (FIRST + 54),            // optional wParam == mask
            GETEXTENDEDLISTVIEWSTYLE = (FIRST + 55),
            GETSUBITEMRECT = (FIRST + 56),
            SUBITEMHITTEST = (FIRST + 57),
            SETCOLUMNORDERARRAY = (FIRST + 58),
            GETCOLUMNORDERARRAY = (FIRST + 59),
            SETHOTITEM = (FIRST + 60),
            GETHOTITEM = (FIRST + 61),
            SETHOTCURSOR = (FIRST + 62),
            GETHOTCURSOR = (FIRST + 63),
            APPROXIMATEVIEWRECT = (FIRST + 64),
            SETWORKAREAS = (FIRST + 65),
            GETWORKAREAS = (FIRST + 70),
            GETNUMBEROFWORKAREAS = (FIRST + 73),
            GETSELECTIONMARK = (FIRST + 66),
            SETSELECTIONMARK = (FIRST + 67),
            SETHOVERTIME = (FIRST + 71),
            GETHOVERTIME = (FIRST + 72),
            SETTOOLTIPS = (FIRST + 74),
            GETTOOLTIPS = (FIRST + 78),
            SORTITEMSEX = (FIRST + 81),
            SETBKIMAGEA = (FIRST + 68),
            SETBKIMAGEW = (FIRST + 138),
            GETBKIMAGEA = (FIRST + 69),
            GETBKIMAGEW = (FIRST + 139),
            SETSELECTEDCOLUMN = (FIRST + 140),
            SETVIEW = (FIRST + 142),
            GETVIEW = (FIRST + 143),
            INSERTGROUP = (FIRST + 145),
            SETGROUPINFO = (FIRST + 147),
            GETGROUPINFO = (FIRST + 149),
            REMOVEGROUP = (FIRST + 150),
            MOVEGROUP = (FIRST + 151),
            GETGROUPCOUNT = (FIRST + 152),
            GETGROUPINFOBYINDEX = (FIRST + 153),
            MOVEITEMTOGROUP = (FIRST + 154),
            GETGROUPRECT = (FIRST + 98),
            SETGROUPMETRICS = (FIRST + 155),
            GETGROUPMETRICS = (FIRST + 156),
            ENABLEGROUPVIEW = (FIRST + 157),
            SORTGROUPS = (FIRST + 158),
            INSERTGROUPSORTED = (FIRST + 159),
            REMOVEALLGROUPS = (FIRST + 160),
            HASGROUP = (FIRST + 161),
            GETGROUPSTATE = (FIRST + 92),
            GETFOCUSEDGROUP = (FIRST + 93),
            SETTILEVIEWINFO = (FIRST + 162),
            GETTILEVIEWINFO = (FIRST + 163),
            SETTILEINFO = (FIRST + 164),
            GETTILEINFO = (FIRST + 165),
            SETINSERTMARK = (FIRST + 166),
            GETINSERTMARK = (FIRST + 167),
            INSERTMARKHITTEST = (FIRST + 168),
            GETINSERTMARKRECT = (FIRST + 169),
            SETINSERTMARKCOLOR = (FIRST + 170),
            GETINSERTMARKCOLOR = (FIRST + 171),
            GETSELECTEDCOLUMN = (FIRST + 174),
            ISGROUPVIEWENABLED = (FIRST + 175),
            GETOUTLINECOLOR = (FIRST + 176),
            SETOUTLINECOLOR = (FIRST + 177),
            CANCELEDITLABEL = (FIRST + 179),
            MAPINDEXTOID = (FIRST + 180),
            MAPIDTOINDEX = (FIRST + 181),
            ISITEMVISIBLE = (FIRST + 182),
            GETACCVERSION = (FIRST + 193),
            GETEMPTYTEXT = (FIRST + 204),
            GETFOOTERRECT = (FIRST + 205),
            GETFOOTERINFO = (FIRST + 206),
            GETFOOTERITEMRECT = (FIRST + 207),
            GETFOOTERITEM = (FIRST + 208),
            GETITEMINDEXRECT = (FIRST + 209),
            SETITEMINDEXSTATE = (FIRST + 210),
            GETNEXTITEMINDEX = (FIRST + 211),
            SETPRESERVEALPHA = (FIRST + 212),
            SETBKIMAGE = SETBKIMAGEW,
            GETBKIMAGE = GETBKIMAGEW,
            //SETBKIMAGE             = SETBKIMAGEA,
            //GETBKIMAGE             = GETBKIMAGEA,
        }

        internal enum LVS
        {
            EX_DOUBLEBUFFER = 0x00010000
        }

        [Description("Merging Application")]
        internal enum Merger
        {
            [Description("No Merging Application")]
            None,

            [Description("Microsoft's IL-Merge")]
            ILMerge,

            [Description("Mono Based IL-Repack")]
            ILRepack
        }

        #endregion Enumerations

        #region Properties

        /// <summary>
        /// List of supported File Extensions and their Descriptions for the ListView groups.
        /// </summary>
        public Dictionary<String, String> Extensions
        {
            get;
            private set;
        }

        /// <summary>
        /// Name of the ILMerge Assembly.
        /// </summary>
        public String ilMerge
        {
            get
            {
                switch (Engine)
                {
                    case Merger.ILMerge:
                        return "ILMerge";
                    case Merger.ILRepack:
                        return "ILRepack";
                    default:
                        return "None";
                }
            }
        }

        /// <summary>
        /// Path of the ILMerge Executable.
        /// </summary>
        public String iLMergePath
        {
            get;
            private set;
        }

        /// <summary>
        /// Filename of the Primary Assembly (exe).
        /// </summary>
        public String Primary
        {
            get;
            private set;
        }

        /// <summary>
        /// The Application's Directory
        /// </summary>
        internal static String AppDir
        {
            get
            {
                return Path.GetDirectoryName(Application.ExecutablePath);
            }
        }

        /// <summary>
        /// The Applications Title. 
        /// 
        /// It is either the ProductName or derived from the 
        /// last part of the Application's Directory.
        /// </summary>
        internal static String AppTitle
        {
            get
            {
                return "ILMergeGUI";

                //! After merge with mru.dll the following code fails.
                //String Result = String.Empty;

                //FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(Application.ExecutablePath);

                ////if (String.IsNullOrEmpty(Application.ProductName))
                //if (String.IsNullOrEmpty(fvi.ProductName)) {
                //    String[] split = AppDir.Split(Path.DirectorySeparatorChar);
                //    Result = Path.ChangeExtension(split[split.Length - 1], "");
                // }
                // else
                // {
                //    Result = fvi.ProductName;

                //    MessageBox.Show(Application.ExecutablePath);
                //    MessageBox.Show(Application.ProductName);
                //}

                //return Result;
            }
        }

        /// <summary>
        /// Type of Merge Executable.
        /// </summary>
        internal Merger Engine
        {
            get;
            set;
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Registers ILMerge File Type.
        /// </summary>
        /// <returns>true if successfull</returns>
        public static Boolean DetectIlProj()
        {
            //TODO Should this be HKEY_CURRENT_USER\Software\Classes or HKEY_LOCAL_MACHINE\Software\Classes?
            //     See http://social.msdn.microsoft.com/Forums/en/netfxbcl/thread/630ed1d9-73f1-4cc0-bc84-04f29cffc13b

            using (RegistryKey rk = Registry.ClassesRoot.OpenSubKey(MyExtension, false))
            {
                if (rk == null)
                {
                    return false;
                }

                // Check Our own filetype
                if (rk.GetValue(null) == null || !rk.GetValue(null).Equals(MyAppName + "_file"))
                {
                    return false;
                }

                // Check Mime type...
                if (rk.GetValue("Content Type") == null || !rk.GetValue("Content Type").Equals(String.Format("application/{0}", MyAppName.ToLower())))
                {
                    return false;
                }

                using (RegistryKey rk2 = Registry.ClassesRoot.OpenSubKey(@"\" + MyAppName + "_file", false))
                {
                    if (rk2 == null)
                    {
                        return false;
                    }

                    // Check Assignment of hottack.net to *.hot files
                    if (rk2.GetValue(null) == null || !rk2.GetValue(null).Equals(MyAppName + " File"))
                    {
                        return false;
                    }

                    // Check editflags
                    if (rk2.GetValue("EditFlags") == null)
                    {
                        return false;
                    }

                    // Check Show extension
                    if (rk2.GetValue("AlwaysShowExt") == null)
                    {
                        return true;
                    }

                    // Check the icon to a large application one
                    using (RegistryKey rk3 = rk2.OpenSubKey("DefaultIcon", false))
                    {
                        if (rk3 == null || rk3.GetValue(null) == null || !rk3.GetValue(null).ToString().Equals(Application.ExecutablePath + ",0", StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }
                    }

                    // Check the open verb
                    using (RegistryKey rk4 = rk2.OpenSubKey(@"shell\open\command", false))
                    {
                        if (rk4 == null || rk4.GetValue(null) == null || !rk4.GetValue(null).ToString().Equals("\"" + Application.ExecutablePath + "\"" + " \"%1\"", StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }
                    }

                    // Check the merge verb
                    using (RegistryKey rk5 = rk2.OpenSubKey(@"shell\merge\command", false))
                    {
                        if (rk5 == null || rk5.GetValue(null) == null || !rk5.GetValue(null).ToString().Equals("\"" + Application.ExecutablePath + "\"" + " \"%1\" /Merge", StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Enumerate available x86 and x64 DotNet Framework Versions.
        /// </summary>
        /// <returns>A list of DotNet version</returns>
        public static List<DotNet> InstalledDotNetVersions()
        {
            List<DotNet> versions = new List<DotNet>();

            //TODO This needs an own routine to decode the SP (as it's part of the version and the installed key is missing anyway).
            RegistryKey ICKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Active Setup\Installed Components");
            if (ICKey != null)
            {
                GetDotNetVersion(ICKey.OpenSubKey("{78705f0d-e8db-4b2d-8193-982bdda15ecd}"), "{78705f0d-e8db-4b2d-8193-982bdda15ecd}", versions);
                GetDotNetVersion(ICKey.OpenSubKey("{FDC11A6F-17D1-48f9-9EA3-9051954BAA24}"), "{FDC11A6F-17D1-48f9-9EA3-9051954BAA24}", versions);
            }

            RegistryKey NDPKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP");
            if (NDPKey != null)
            {
                string[] subkeys = NDPKey.GetSubKeyNames();
                foreach (string subkey in subkeys)
                {
                    //! 16-11-2013 - veg - Added.
                    if (NDPKey.OpenSubKey(subkey).GetValue("") != null && NDPKey.OpenSubKey(subkey).GetValue("").Equals("deprecated"))
                    {
                        continue;
                    }
                    GetDotNetVersion(NDPKey.OpenSubKey(subkey), subkey, versions);
                    GetDotNetVersion(NDPKey.OpenSubKey(subkey).OpenSubKey("Client"), subkey, versions);
                    GetDotNetVersion(NDPKey.OpenSubKey(subkey).OpenSubKey("Full"), subkey, versions);
                }
            }

            return versions;
        }

        /// <summary> 
        /// Creates a relative path from one file or folder to another. 
        /// 
        /// See http://stackoverflow.com/questions/275689/how-to-get-relative-path-from-absolute-path
        /// </summary> 
        /// <param name="fromPath">Contains the directory that defines the start of the relative path.</param> 
        /// <param name="toPath">Contains the path that defines the endpoint of the relative path.</param> 
        /// <returns>The relative path from the start directory to the end path.</returns> 
        /// <exception cref="ArgumentNullException">One of the strings is empty</exception> 
        public static String MakeRelativePath(String fromPath, String toPath)
        {
            if (String.IsNullOrEmpty(fromPath))
                throw new ArgumentNullException("fromPath");
            if (String.IsNullOrEmpty(toPath))
                throw new ArgumentNullException("toPath");

            Uri fromUri = new Uri(fromPath);
            Uri toUri = new Uri(toPath);

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            String relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (String.IsNullOrEmpty(relativePath))
            {
                return Path.GetFileName(toPath);
            }
            else
            {
                return relativePath.Replace('/', Path.DirectorySeparatorChar);
            }
        }

        /// <summary>
        /// Register File Type and Application.
        /// </summary>
        /// <returns>True if successfull</returns>
        public static Boolean RegisterIlProj()
        {
            if (DetectIlProj())
            {
                return true;
            }

            try
            {
                using (RegistryKey rk1 = Registry.ClassesRoot.CreateSubKey(MyExtension))
                {

                    // Register our own filetype
                    if (rk1 != null)
                    {
                        // Assign hottack.net to *.hot files
                        if (rk1.GetValue(null) == null ||
                            !rk1.GetValue(null).Equals(MyAppName + "_file"))
                        {
                            //(Default) value.
                            rk1.SetValue(null, MyAppName + "_file");
                        }

                        // Propose a new mime type...
                        if (rk1.GetValue("Content Type") == null ||
                            !rk1.GetValue("Content Type").Equals(String.Format("application/{0}", MyAppName.ToLower())))
                        {
                            rk1.SetValue("Content Type", String.Format("application/{0}", MyAppName.ToLower()));
                        }
                    }
                    else
                    {
                        return false;
                    }

                    using (RegistryKey rk2 = Registry.ClassesRoot.CreateSubKey(MyAppName + "_file"))
                    {
                        // Register our own filetype
                        if (rk2 != null)
                        {
                            // Assign hottack.net to *.hot files
                            if (rk2.GetValue(null) == null ||
                                !rk2.GetValue(null).Equals(MyAppName + " File"))
                            {
                                rk2.SetValue(null, MyAppName + " File");
                            }

                            // Purpose of editflags unknown
                            if (rk2.GetValue("EditFlags") == null)
                            {
                                rk2.SetValue("EditFlags", new Byte[4] { 0, 0, 0, 0 }, RegistryValueKind.Binary);
                            }

                            // Always show extension
                            if (rk2.GetValue("AlwaysShowExt") == null)
                            {
                                rk2.SetValue("AlwaysShowExt", String.Empty);
                            }

                            // Change the icon to a large application one
                            using (RegistryKey rk3 = rk2.CreateSubKey("DefaultIcon"))
                            {
                                if (rk3 != null)
                                {
                                    if (rk3.GetValue(null) == null ||
                                        !rk3.GetValue(null).Equals(Application.ExecutablePath + ",0"))
                                    {
                                        rk3.SetValue(null, Application.ExecutablePath + ",0");
                                    }
                                }
                                else
                                {
                                    return false;
                                }

                            }

                            // Add the open verb
                            using (RegistryKey rk4 = rk2.CreateSubKey(@"shell\open\command"))
                            {
                                if (rk4 != null)
                                {
                                    if (rk4.GetValue(null) == null ||
                                        !rk4.GetValue(null).Equals("\"" + Application.ExecutablePath + "\"" + " \"%1\""))
                                    {
                                        rk4.SetValue(null, "\"" + Application.ExecutablePath + "\"" + " \"%1\"");
                                    }
                                }
                                else
                                {
                                    return false;
                                }
                            }

                            // Add the merge verb
                            using (RegistryKey rk4 = rk2.CreateSubKey(@"shell\merge\command"))
                            {
                                if (rk4 != null)
                                {
                                    if (rk4.GetValue(null) == null ||
                                        !rk4.GetValue(null).Equals("\"" + Application.ExecutablePath + "\"" + " \"%1\" /Merge"))
                                    {
                                        rk4.SetValue(null, "\"" + Application.ExecutablePath + "\"" + " \"%1\" /Merge");
                                    }
                                }
                                else
                                {
                                    return false;
                                }
                            }

                            //TODO ilproj_auto_file|shell|open|command|(Default)=Executable?
                            //     Not clear what creates this key. Created by Explorer's Open Width Context MenuItem?

                            //TODO HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.ilproj
                            //     If old code does not work, above key is probably present (controlled by Explorer's Open Width Context MenuItem).

                            //TODO HKEY_CLASSES_ROOT\Applications\ILMergeGui.exe
                            //     Created by Explorer's Open Width Context MenuItem.

                            //{$IFDEF SHLWAPI}
                            //            //Let Windows Udate Explorer
                            //            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, nil, nil);
                            //{$ENDIF SHLWAPI}
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Search for DotNet version and path information.
        /// </summary>
        /// <param name="parentKey">The Registry Key.</param>
        /// <param name="versionKey">The sub version Name.</param>
        /// <param name="versions">The list of DotNet versions.</param>
        private static void GetDotNetVersion(RegistryKey parentKey, string versionKey, List<DotNet> versions)
        {
            if (parentKey != null)
            {
                string installed = Convert.ToString(parentKey.GetValue("Install"));
                if (installed == "1")
                {
                    string version = Convert.ToString(parentKey.GetValue("Version"));
                    if (string.IsNullOrEmpty(version))
                    {
                        if (versionKey.StartsWith("v"))
                            version = versionKey.Substring(1);
                        else
                            version = versionKey;
                    }

                    Version ver = new Version(version);

                    String x64syspath = String.Empty;
                    String x86syspath = String.Empty;

                    String x64pfpath = String.Empty;
                    String x86pfpath = String.Empty;

                    if (Environment.Is64BitOperatingSystem)
                    {
                        x64syspath = Convert.ToString(parentKey.GetValue("InstallPath"));
                    }
                    else
                    {
                        x86syspath = Convert.ToString(parentKey.GetValue("InstallPath"));
                    }

                    //Test for x64 directory names.
                    if (Environment.Is64BitOperatingSystem)
                    {
                        x64syspath = TestDotnetPath(
                            ver,
                            x64syspath,
                            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                                                          @"Microsoft.NET\Framework64\"));

                        x64pfpath = TestDotnetPath(
                            ver,
                            x64pfpath,
                            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                                                      @"Reference Assemblies\Microsoft\Framework\"));
                    }

                    //Test for x86 directory names.
                    x86syspath = TestDotnetPath(
                         ver,
                         x86syspath,
                         Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                                                       @"Microsoft.NET\Framework\"));

                    //Test for x86 directory names.
                    x86pfpath = TestDotnetPath(
                        ver,
                        x86pfpath,
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                                                      @"Reference Assemblies\Microsoft\Framework\"));

                    String pattern = ".NET Framework {0}";

                    if (parentKey.GetValue("SP") != null)
                    {
                        pattern += " Service Pack " + Convert.ToString(parentKey.GetValue("SP"));
                    }

                    //HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Client
                    String type = parentKey.Name.Substring(parentKey.Name.LastIndexOf('\\') + 1);

                    if (type.Equals(versionKey))
                    {
                        versions.Add(
                            new DotNet()
                            {
                                //! Add Key for ILRepack.
                                key = versionKey,
                                name = String.Format(pattern, versionKey),
                                version = ver,
                                x86WindowsPath = x86syspath.TrimEnd(Path.DirectorySeparatorChar),
                                x86ProgramFilesPath = x86pfpath.TrimEnd(Path.DirectorySeparatorChar),
                                x64WindowsPath = x64syspath.TrimEnd(Path.DirectorySeparatorChar),
                                x64ProgramFilesPath = x64pfpath.TrimEnd(Path.DirectorySeparatorChar),
                            });

                        // String.Format(pattern, subVersionName), ver);
                    }
                    else
                    {
                        //! .Net v4.5 fixup.
                        //! [workitem:8745]
                        //! .Net v4.6 fixup.
                        //! [workitem:8753]
                        if (ver.Major == 4 && (ver.Minor == 5 || ver.Minor == 6))
                        {
                            versionKey = String.Format("4.{0}", ver.Minor);
                            versions.Add(new DotNet()
                            {
                                //! Add Key for ILRepack.
                                key = "v4",
                                name = String.Format(pattern, versionKey + " " + type),
                                version = ver,
                                x86WindowsPath = x86syspath.TrimEnd(Path.DirectorySeparatorChar),
                                x86ProgramFilesPath = x86pfpath.TrimEnd(Path.DirectorySeparatorChar),
                                x64WindowsPath = x64syspath.TrimEnd(Path.DirectorySeparatorChar),
                                x64ProgramFilesPath = x64pfpath.TrimEnd(Path.DirectorySeparatorChar),
                            });
                        }
                        else
                        {
                            versions.Add(new DotNet()
                                {
                                    key = versionKey,
                                    name = String.Format(pattern, versionKey + " " + type),
                                    version = ver,
                                    x86WindowsPath = x86syspath.TrimEnd(Path.DirectorySeparatorChar),
                                    x86ProgramFilesPath = x86pfpath.TrimEnd(Path.DirectorySeparatorChar),
                                    x64WindowsPath = x64syspath.TrimEnd(Path.DirectorySeparatorChar),
                                    x64ProgramFilesPath = x64pfpath.TrimEnd(Path.DirectorySeparatorChar),
                                });
                        }
                    }
                }
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, UInt32 wParam, UInt32 lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, UInt32 lParam);

        private static String TestDotnetPath(Version ver, String frameworkpath, String basepath)
        {
            if (String.IsNullOrEmpty(frameworkpath))
            {
                String path = Path.Combine(basepath, String.Format("v{0}.{1}", ver.Major, ver.Minor));
                if (Directory.Exists(path))
                {
                    return path;
                }
            }

            if (String.IsNullOrEmpty(frameworkpath))
            {
                String path = Path.Combine(basepath, String.Format("v{0}.{1}.{2}", ver.Major, ver.Minor, ver.Build));
                if (Directory.Exists(path))
                {
                    return path;
                }
            }

            return frameworkpath;
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new AboutDialog().ShowDialog();
        }

        private void btnAddFile_Click(object sender, EventArgs e)
        {
            openFile1.CheckFileExists = true;
            openFile1.Multiselect = true;
            openFile1.FileName = String.Empty;
            openFile1.Filter = ".NET Assembly|*.dll;*.exe";

            if (openFile1.ShowDialog() == DialogResult.OK)
            {
                ProcessFiles(openFile1.FileNames);
            }
        }

        private void btnKeyFile_Click(object sender, EventArgs e)
        {
            SelectKeyFile();
        }

        private void btnLogFile_Click(object sender, EventArgs e)
        {
            SelectLogFile();
        }

        private void btnMerge_Click(object sender, EventArgs e)
        {
            //! Locate ILMerge.exe on disk or registry.
            //! Load ILMerge.exe dynamically.
            //! c:\Program Files (x86)\Microsoft\ILMerge\ILMerge.exe

            //! HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S-1-5-21-822211721-2317658140-2171821640-1000\Components\F995DC6782BCD301ECDB40AF0BEFB501
            //! FB8E12458022DA64AB4CCF9364EE3B15=C:\Program Files (x86)\Microsoft\ILMerge\ILMerge.exe
            //! or
            //! HKEY_CURRENT_USER\Software\Microsoft\Installer\Assemblies\C:|Program Files (x86)|Microsoft|ILMerge|ILMerge.exe
            //! just enumerate
            //! HKEY_CURRENT_USER\Software\Microsoft\Installer\Assemblies until a key ends in |ILMerge.exe

            //Debug.WriteLine(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86));
            //Debug.WriteLine(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles));

            if (Engine == Merger.None)
            {
                return;
            }

            //! TODO Nicely find ILMerge.
            if (!DynaInvoke.PreLoadAssembly(iLMergePath, ilMerge))
            {
                return;
            }

            arrAssemblies = new List<String>();

            PreMerge();

            if (!String.IsNullOrWhiteSpace(TxtOutputAssembly.Text) && !Directory.Exists(Path.GetDirectoryName(TxtOutputAssembly.Text.Trim())))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(TxtOutputAssembly.Text.Trim()));
            }

            //! [workitem:8741]
            if (String.IsNullOrWhiteSpace(TxtOutputAssembly.Text) || !Directory.Exists(Path.GetDirectoryName(TxtOutputAssembly.Text.Trim())))
            {
                ExitMsg = Resources.Error_NoOutputPath;
                ExitCode = 8;
                
                if (!AutoClose)
                {
                    MessageBox.Show(ExitMsg, Resources.Error_Term, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    ExitILMergeGUI();
                }

                TxtOutputAssembly.Focus();

                return;
            }
            else
            {
                TxtOutputAssembly.Text = TxtOutputAssembly.Text.Trim();
            }

            if (File.Exists(TxtKeyFile.Text) && !File.Exists(TxtKeyFile.Text))
            {
                ExitMsg = Resources.Error_KeyFileNotExists;
                ExitCode = 7;
                if (!AutoClose)
                {
                    MessageBox.Show(ExitMsg, Resources.Error_Term, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    ExitILMergeGUI();
                }
                return;
            }

            for (Int32 i = 0; i < ListAssembly.Items.Count; i++)
            {
                if (((String)ListAssembly.Items[i].Tag).ToLower().Equals(TxtOutputAssembly.Text.ToLower()))
                {
                    ExitMsg = Resources.Error_OutputConflict;
                    ExitCode = 6;

                    if (!AutoClose)
                    {
                        MessageBox.Show(ExitMsg, Resources.Error_Term, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        ExitILMergeGUI();
                    }
                    TxtOutputAssembly.Focus();

                    return;
                }
            }

            if (File.Exists(TxtOutputAssembly.Text))
            {
                try
                {
                    FileInfo objFile = new FileInfo(TxtOutputAssembly.Text);
                    objFile.Attributes = FileAttributes.Normal;
                    objFile.Delete();
                    objFile = null;
                }
                catch (Exception)
                {
                    ExitMsg = Resources.Error_OutputPathInUse;
                    ExitCode = 5;

                    if (!AutoClose)
                    {
                        MessageBox.Show(ExitMsg, Resources.Error_Term, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        ExitILMergeGUI();
                    }
                    return;
                }
            }

            if (ListAssembly.SelectedItems.Count == 0)
            {
                for (Int32 i = 0; i < ListAssembly.Items.Count; i++)
                {
                    if (Path.GetExtension((String)ListAssembly.Items[i].Tag).ToLower() == ".exe")
                    {
                        ListAssembly.Items[i].Selected = true;
                        break;
                    }
                }
            }

            if (ListAssembly.SelectedItems.Count == 0)
            {
                ListAssembly.Items[0].Selected = true;
            }

            arrAssemblies.Add(Primary);
            for (Int32 i = 0; i < ListAssembly.Items.Count; i++)
            {
                if (!arrAssemblies.Contains((String)ListAssembly.Items[i].Tag))
                {
                    arrAssemblies.Add((String)ListAssembly.Items[i].Tag);
                }
            }

            Console.WriteLine("{0}.{1}={1}", ilMerge, "CopyAttributes", DynaInvoke.SetProperty<Boolean>(iLMergePath, ilMerge, "CopyAttributes", ChkCopyAttributes.Checked));
            Console.WriteLine("{0}.{1}={1}", ilMerge, "UnionMerge", DynaInvoke.SetProperty<Boolean>(iLMergePath, ilMerge, "UnionMerge", ChkUnionDuplicates.Checked));

            Console.WriteLine("{0}.{1}={1}", ilMerge, "Internalize", DynaInvoke.SetProperty<Boolean>(iLMergePath, ilMerge, "Internalize", ChkInternalize.Checked));

            Console.WriteLine("{0}.{1}={1}", ilMerge, "XmlDocumentation", DynaInvoke.SetProperty<Boolean>(iLMergePath, ilMerge, "XmlDocumentation", ChkMergeXml.Checked));

            if (ChkSignKeyFile.Checked)
            {
                Console.WriteLine("{0}.{1}={2}", ilMerge, "KeyFile", DynaInvoke.SetProperty<String>(iLMergePath, ilMerge, "KeyFile", TxtKeyFile.Text));
                Console.WriteLine("{0}.{1}={2}", ilMerge, "DelaySign", DynaInvoke.SetProperty<Boolean>(iLMergePath, ilMerge, "DelaySign", ChkDelayedSign.Checked));
            }

            if (ChkGenerateLog.Checked)
            {
                Console.WriteLine("{0}.{1}={2}", ilMerge, "Log", DynaInvoke.SetProperty<Boolean>(iLMergePath, ilMerge, "Log", ChkGenerateLog.Checked).ToString());
                Console.WriteLine("{0}.{1}={2}", ilMerge, "LogFile", DynaInvoke.SetProperty(iLMergePath, ilMerge, "LogFile", TxtLogFile.Text));
            }

            Console.WriteLine("{0}.{1}={2}", ilMerge, "DebugInfo", DynaInvoke.SetProperty<Boolean>(iLMergePath, ilMerge, "DebugInfo", CboDebug.SelectedIndex == 0 ? true : false));

            //! This must be done better!!
            //! TODO Enumerate Frameworks and find correct directories under Program Files.

            framework = (DotNet)CboTargetFramework.SelectedItem;

            frameversion = String.Format("{0}.{1}", framework.version.Major, framework.version.Minor);

            //! .Net v4.5 fixup. 4.5 is an inplace upgrade of 4.0 which does not alter the version number.
            //! [workitem:8745]

            //! .Net v4.6 fixup.. 4.6 is an inplace upgrade of 4.0 which does not alter the version number.
            //! [workitem:8753]
            if (Engine == Merger.ILMerge && framework.version.Major == 4 && (framework.version.Minor == 5 || framework.version.Minor == 6 || framework.version.Minor == 7 || framework.version.Minor == 8))
            {
                frameversion = "4.0";
            }

            String ff = frameversion;

            //! Fixups for ILRepack (does not support .NET 3/3.5 or 4.5 it seems),
            //! It uses the registry keys and does not support v2.0, v3.0, v3.5 or v4.5.
            //
            //  case "v1":   runtime = TargetRuntime.Net_1_0; break;
            //  case "v1.1": runtime = TargetRuntime.Net_1_1; break;
            //  case "v2":   runtime = TargetRuntime.Net_2_0; break;
            //  case "v4":   runtime = TargetRuntime.Net_4_0; break;
            //
            if (Engine == Merger.ILRepack)
            {
                //! 16-11-2013 - veg - Workaround for Stack Overflow in ILRepack!
                frameversion = null;

                //! String.Format("v{0}", framework.version.Major);
            }

            try
            {
                if (Environment.Is64BitOperatingSystem)
                {
                    Console.WriteLine("{0}.{1}('{2}', '{3}')",
                        ilMerge,
                        "SetTargetPlatform",
                        frameversion,
                        framework.x64WindowsPath);
                    DynaInvoke.CallMethod<Object>(iLMergePath, ilMerge, "SetTargetPlatform", new String[] { frameversion, framework.x64WindowsPath });
                }
                else
                {
                    Console.WriteLine("{0}.{1}('{2}', '{3}')",
                        ilMerge,
                        "SetTargetPlatform",
                        frameversion,
                        framework.x86WindowsPath);
                    DynaInvoke.CallMethod<Object>(iLMergePath, ilMerge, "SetTargetPlatform", new String[] { frameversion, framework.x86WindowsPath });
                }
            }
            catch (TargetInvocationException)
            {
                ExitMsg = String.Format(Resources.Error_Framework, ff, ilMerge);
                ExitCode = 9;

                if (!AutoClose)
                {
                    MessageBox.Show(ExitMsg, Resources.Error_Term, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    ExitILMergeGUI();
                }

                return;
            }

            //Console.WriteLine("{0}.{1}={2}",ilMerge, "Version", DynaInvoke.GetProperty<Version>(iLMergePath, ilMerge, "Version"));

            //0=Dll
            //1=Exe
            //2=WinExe
            //3=ILMerge.Kind.SameAsPrimaryAssembly

            //public enum Kind
            //{
            //    Dll,                  //0
            //    Exe,                  //1
            //    WinExe,               //2
            //    SameAsPrimaryAssembly //3
            //}

            //fix for issue: 8737
            switch (Engine)
            {
                case Merger.ILMerge:
                    Console.WriteLine("{0}.{1}={2}", ilMerge, "TargetKind", DynaInvoke.SetProperty<Int32>(iLMergePath, ilMerge, "TargetKind", 3));
                    break;
                case Merger.ILRepack:
                    //! Fix for ILRepack which uses a Nullable Enum.
                    Console.WriteLine("{0}.{1}={2}", ilMerge, "TargetKind", DynaInvoke.SetProperty<Int32?>(iLMergePath, ilMerge, "TargetKind", (Int32?)3));
                    break;
            }
            Console.WriteLine("{0}.{1}={2}", ilMerge, "OutputFile", DynaInvoke.SetProperty<String>(iLMergePath, ilMerge, "OutputFile", TxtOutputAssembly.Text));

            // Clear input assemblies.
            Console.WriteLine("{0}.{1}(", ilMerge, "SetInputAssemblies");
            foreach (String asm in arrAssemblies)
            {
                Console.WriteLine("                           '{0}'", asm);
            }
            Console.WriteLine("                          )");

            DynaInvoke.CallMethod<Object>(iLMergePath, ilMerge, "SetInputAssemblies", new Object[] { arrAssemblies.ToArray() });

            if (ChkGenCmdLine.Checked)
            {
                String cmdFile = Path.Combine(
                        Path.GetDirectoryName(TxtOutputAssembly.Text.Trim()),
                        Path.ChangeExtension(Path.GetFileName(TxtOutputAssembly.Text.Trim()), ".txt"));

                File.WriteAllText(cmdFile, DoGenerateCmdLine());

                Process.Start(new ProcessStartInfo(cmdFile));
            }

            //Cursor = Cursors.WaitCursor
            EnableForm(false);

            WorkerILMerge.RunWorkerAsync();
        }

        private String DoGenerateCmdLine()
        {
            StringBuilder sb = new StringBuilder();

            // ilmerge 
            sb.AppendLine(String.Format("\"{0}\"", iLMergePath));

            //! [/lib:directory]* 

            // [/log[:filename]] 
            if (DynaInvoke.GetProperty<Boolean>(iLMergePath, ilMerge, "Log"))
            {
                if (DynaInvoke.GetProperty<Boolean>(iLMergePath, ilMerge, "LogFile"))
                {
                    sb.AppendLine(String.Format("/log:\"{0}\"", DynaInvoke.GetProperty<Boolean>(iLMergePath, ilMerge, "LogFile")));
                }
                else
                {
                    sb.AppendLine("/log");
                }
            }

            // [/keyfile:filename [/delaysign]] 
            if (ChkSignKeyFile.Checked)
            {
                sb.AppendLine(String.Format("/keyfile:\"{0}\"", DynaInvoke.GetProperty<String>(iLMergePath, ilMerge, "KeyFile")));
                if (DynaInvoke.GetProperty<Boolean>(iLMergePath, ilMerge, "DelaySign"))
                {
                    sb.AppendLine("/delaysign");
                }
            }

            // [/internalize[:filename]] 
            if (DynaInvoke.GetProperty<Boolean>(iLMergePath, ilMerge, "Internalize"))
            {
                sb.AppendLine("/internalize");
            }

            // [/t[arget]:(library|exe|winexe)] 
            Int32 t = 3;
            switch (t)
            {
                // Dll,                  0
                case 0:
                    sb.AppendLine("/target:library");
                    break;
                // Exe,                  1
                case 1:
                    sb.AppendLine("/target:exe");
                    break;
                // WinExe,               2
                case 2:
                    sb.AppendLine("/target:winexe");
                    break;
                // SameAsPrimaryAssembly 3
                case 3:
                    // sb.AppendLine("/target:SameAsPrimaryAssembly");
                    break;
            }

            //! [/closed] 

            // [/ndebug] 
            if (!DynaInvoke.GetProperty<Boolean>(iLMergePath, ilMerge, "DebugInfo"))
            {
                sb.AppendLine("/ndebug");
            }

            //! [/ver:version] 

            // [/copyattrs [/allowMultiple] [/keepFirst]] 
            if (DynaInvoke.GetProperty<Boolean>(iLMergePath, ilMerge, "CopyAttributes"))
            {
                sb.AppendLine("/copyattrs");
            }

            // [/xmldocs] 
            if (DynaInvoke.GetProperty<Boolean>(iLMergePath, ilMerge, "XmlDocumentation"))
            {
                sb.AppendLine("/xmldocs");
            }

            //! [/attr:filename] 

            // [/targetplatform:<version>[,<platformdir>] | /v1 | /v1.1 | /v2 | /v4] 
            sb.AppendLine(String.Format("/targetplatform:{0},\"{1}\"", frameversion, Environment.Is64BitOperatingSystem ? framework.x64WindowsPath : framework.x86WindowsPath));

            //! [/useFullPublicKeyForReferences] 

            //! [/wildcards] 

            //! [/zeroPeKind] 

            //! [/allowDup:type]*

            // [/union] 
            if (DynaInvoke.GetProperty<Boolean>(iLMergePath, ilMerge, "UnionMerge"))
            {
                sb.AppendLine("/union");
            }

            //! [/align:n] 

            // /out:filename 
            sb.AppendLine(String.Format("/out:\"{0}\"", DynaInvoke.GetProperty<String>(iLMergePath, ilMerge, "OutputFile")));

            //<primary assembly> [<other assemblies>...]
            sb.AppendLine();
            sb.AppendLine(String.Format("     \"{0}\"", arrAssemblies.First()));
            sb.AppendLine();
            foreach (String asm in arrAssemblies.Skip(1))
            {
                sb.AppendLine(String.Format("     \"{0}\"", asm));
            }

            return sb.ToString();
        }

        private void btnOutputPath_Click(object sender, EventArgs e)
        {
            SelectOutputFile();
        }

        private void CboDebug_SelectedIndexChanged(object sender, EventArgs e)
        {
            //
        }

        private void checkForUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClickOnceUpdater.InstallUpdateSyncWithInfo("http://ilmergegui.codeplex.com/releases/view/latest");
        }

        private void ChkGenerateLog_CheckedChanged(object sender, EventArgs e)
        {
            TxtLogFile.Enabled = ChkGenerateLog.Checked;
            btnLogFile.Enabled = ChkGenerateLog.Checked;
        }

        private void ChkSignKeyFile_CheckedChanged(object sender, EventArgs e)
        {
            TxtKeyFile.Enabled = ChkSignKeyFile.Checked;
            ChkDelayedSign.Enabled = ChkSignKeyFile.Checked;
            btnKeyFile.Enabled = ChkSignKeyFile.Checked;
        }

        private void EnableForm(bool Enable)
        {
            ListAssembly.Enabled = Enable;
            btnAddFile.Enabled = Enable;
            BoxOptions.Enabled = Enable;
            BoxOutput.Enabled = Enable;
            btnMerge.Enabled = Enable;

            Application.DoEvents();
        }

        private void FormatItems()
        {
            foreach (ListViewItem lvi in ListAssembly.Items)
            {
                //! TODO Add this again, put full path in Tag?
                if (!String.IsNullOrEmpty(Primary))
                {
                    //String bp = Path.GetDirectoryName(Primary);
                    //e.Value = e.Value.ToString().Replace(bp, String.Empty);
                    lvi.Text = MakeRelativePath(Primary, (String)lvi.Tag);
                }
                else
                {
                    lvi.Text = Path.GetFileName((String)lvi.Tag);
                }
            }

            ListAssembly.Columns[0].Width = -1;
        }

        private void LblPrimaryAssembly_TextChanged(object sender, EventArgs e)
        {
            FormatItems();
        }

        private void LinkILMerge_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(LinkILMerge.Text);
        }

        private void ListAssembly_DragDrop(object sender, DragEventArgs e)
        {
            ProcessFiles((String[])e.Data.GetData(DataFormats.FileDrop));
        }

        private void ListAssembly_DragEnter(object sender, DragEventArgs e)
        {
            // make sure they're actually dropping files (not text or anything else)
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false) == true)
            {
                // allow them to continue
                // (without this, the cursor stays a "NO" symbol
                e.Effect = DragDropEffects.All;
            }
        }

        private void ListAssembly_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (e.NewValue == CheckState.Checked)
            {
                ListAssembly.BeginUpdate();
                foreach (ListViewItem lvi in ListAssembly.CheckedItems)
                {
                    if (lvi.Index != e.Index)
                    {
                        lvi.Checked = false;
                        lvi.Selected = false;
                    }
                }

                foreach (ListViewItem lvi in ListAssembly.Items)
                {
                    lvi.Selected = lvi.Index == e.Index;
                }

                ListAssembly.EndUpdate();
            }
        }

        private void ListAssembly_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            UpdatePrimary();
        }

        private void ListAssembly_KeyDown(object sender, KeyEventArgs e)
        {
            Int32 ndx = 0;
            if (e.KeyCode == Keys.Delete &&
                ListAssembly.SelectedItems != null)
            {
                while (ListAssembly.SelectedItems.Count > 0)
                {
                    ndx = ListAssembly.SelectedIndices[0];

                    ListAssembly.Items[ndx].Selected = false;

                    ListAssembly.Items.RemoveAt(ndx);
                }

                if (ndx > 0 || ListAssembly.Items.Count > 0)
                {
                    ListAssembly.Items[Math.Max(0, ndx - 1)].Selected = true;
                }

                btnMerge.Enabled = ListAssembly.Items.Count > 1;
                FormatItems();
            }

            SetWaterMark(ListAssembly.Items.Count == 0);
        }

        private void LocateEngine(Merger merger)
        {
            switch (merger)
            {
                case Merger.ILMerge:
                    LocateIlMerge();
                    break;
                case Merger.ILRepack:
                    LocateIlRepack();
                    break;
            }

            if (File.Exists(iLMergePath))
            {
                label1.Text = String.Format("{0}: v{1}", Path.GetFileNameWithoutExtension(iLMergePath), AssemblyName.GetAssemblyName(iLMergePath).Version.ToString());
            }
            else
            {
                label1.Text = String.Format("{0}: {1}", merger.ToString(), "not found.");
            }
        }

        private void LocateEngines()
        {
            Engine = Merger.None;

            LocateEngine(Merger.ILRepack);
            LocateEngine(Merger.ILMerge);

            switch (Engine)
            {
                case Merger.ILMerge:
                    radioButton1.Checked = true;
                    break;
                case Merger.ILRepack:
                    radioButton2.Checked = true;
                    break;
            }

            if (Engine != Merger.None)
            {
                LocateEngine(Engine);
            }

            if (String.IsNullOrEmpty(iLMergePath) || !File.Exists(iLMergePath))
            {
                MessageBox.Show("IlMerge/Repack could not be located, please reinstall ILMerge/Repack!", "ILMergeGui", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LocateIlMerge()
        {
            Debug.Print("[ILMerge]");

            iLMergePath = String.Empty;

            // 1) Default Installation Locations...
            if (Environment.Is64BitOperatingSystem)
            {
                iLMergePath = @Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    @"Microsoft\ILMerge\ILMerge.exe");
                Debug.Print("ILMerge Location Method=Default x86 install location");
                Debug.Print("ILMerge Path={0}", iLMergePath);
            }
            else
            {
                iLMergePath = @Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    @"Microsoft\ILMerge\ILMerge.exe");
                Debug.Print("ILMerge Location Method=Default install location");
                Debug.Print("ILMerge Path={0}", iLMergePath);
            }

            // 2) Search Path...
            if (String.IsNullOrEmpty(ilMerge) || !File.Exists(iLMergePath))
            {
                //See http://stackoverflow.com/questions/5578385/assembly-searchpath-via-path-environment
                String path = System.Environment.GetEnvironmentVariable("Path");
                String[] folders = path.Split(';');
                foreach (String folder in folders)
                {
                    if (Directory.Exists(folder))
                    {
                        foreach (String file in Directory.GetFiles(folder, "ILMerge.exe"))
                        {
                            iLMergePath = Path.Combine(folder, file);
                            Debug.Print("ILMerge Location Method=%Path%");
                            Debug.Print("ILMerge Path={0}", iLMergePath);
                            break;
                        }
                    }
                    else
                    {
                        //! These folders are missing on the FileSyetem.
                        //Debug.Print(folder);
                    }
                }
            }

            //3) Search Registry...
            if (String.IsNullOrEmpty(iLMergePath) || !File.Exists(iLMergePath))
            {
                //! HKEY_CURRENT_USER\Software\Microsoft\Installer\Assemblies\C:|Program Files (x86)|Microsoft|ILMerge|ILMerge.exe

                //! just enumerate
                //! HKEY_CURRENT_USER\Software\Microsoft\Installer\Assemblies until a key ends in |ILMerge.exe

                using (RegistryKey AssembliesKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Installer\Assemblies", false))
                {
                    // If the return value is null, the key doesn't exist
                    if (AssembliesKey != null)
                    {
                        foreach (String KeyName in AssembliesKey.GetSubKeyNames())
                        {
                            if (KeyName.EndsWith("ILMerge.exe", StringComparison.OrdinalIgnoreCase))
                            {
                                if (File.Exists(KeyName.Replace('|', '\\')))
                                {
                                    iLMergePath = KeyName.Replace('|', '\\');
                                    Debug.Print("ILMerge Location Method=Registry");
                                    Debug.Print("ILMerge Path={0}", iLMergePath);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            //4) Current Directory...
            if (File.Exists("ILMerge.exe"))
            {
                iLMergePath = Path.GetFullPath("ILMerge.exe");

                Debug.Print("ILMerge Location Method=Current Directory");
                Debug.Print("ILMerge Path={0}", iLMergePath);
            }

            //! TODO Fifth Search Strategy.
            //! HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S-1-5-21-822211721-2317658140-2171821640-1000\Components\F995DC6782BCD301ECDB40AF0BEFB501
            //! FB8E12458022DA64AB4CCF9364EE3B15=C:\Program Files (x86)\Microsoft\ILMerge\ILMerge.exe

            if (String.IsNullOrEmpty(iLMergePath) || !File.Exists(iLMergePath))
            {
                Debug.Print("ILMerge Location=Error");
                Debug.Print("ILMerge Path={0}", iLMergePath);

                // MessageBox.Show("IlMerge could not be located, please reinstall!", "ILMergeGui", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                Engine = Merger.ILMerge;

                radioButton1.Enabled = true;
            }

            Debug.Print(String.Empty);
        }

        private void LocateIlRepack()
        {
            iLMergePath = String.Empty;

            Debug.Print("[ILRepack]");

            if (File.Exists(@".\ILRepack.exe"))
            {
                iLMergePath = Path.GetFullPath(@".\ILRepack.exe");

                Debug.Print("ILRepack Location Method=Current Directory");
                Debug.Print("ILRepack Path={0}", iLMergePath);
            }
            else
            {
                String path = System.Environment.GetEnvironmentVariable("Path");
                String[] folders = path.Split(';');
                foreach (String folder in folders)
                {
                    if (Directory.Exists(folder))
                    {
                        foreach (String file in Directory.GetFiles(folder, "ILRepack.exe"))
                        {
                            iLMergePath = Path.Combine(folder, file);
                            Debug.Print("ILRepack Location Method=%Path%");
                            Debug.Print("ILRepack Path={0}", iLMergePath);
                            break;
                        }
                    }
                    else
                    {
                        //! These folders are missing on the FileSyetem.
                        //Debug.Print(folder);
                    }
                }
            }

            if (String.IsNullOrEmpty(iLMergePath))
            {
                Debug.Print("ILRepack not found");
            }
            else
            {
                Engine = Merger.ILRepack;

                radioButton2.Enabled = true;
            }

            Debug.Print(String.Empty);
        }

        private void Mainform_Load(object sender, EventArgs e)
        {
            Boolean registered = RegisterIlProj();

            Extensions = new Dictionary<String, String>();
            Extensions.Add("exe", "Executable(s)");
            Extensions.Add("dll", "Assemblies or dll(s)");

            openFileDialog1.DefaultExt = MyWildcard;
            openFileDialog1.FileName = MyWildcard;
            openFileDialog1.Filter = String.Format("IlMerge Project|{0}|All Files|*.*", MyWildcard);

            saveFileDialog1.DefaultExt = MyWildcard;
            saveFileDialog1.FileName = MyWildcard;
            saveFileDialog1.Filter = String.Format("IlMerge Project|{0}|All Files|*.*", MyWildcard);

            ListAssembly.Columns[0].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);

            foreach (KeyValuePair<String, String> kvp in Extensions)
            {
                ListAssembly.Groups.Add(kvp.Key, kvp.Value);
            }

            SendMessage(ListAssembly.Handle, (int)LVM.SETTEXTBKCOLOR, IntPtr.Zero, CLR_NONE);
            SendMessage(ListAssembly.Handle, (int)LVM.SETEXTENDEDLISTVIEWSTYLE, (int)LVS.EX_DOUBLEBUFFER, (int)LVS.EX_DOUBLEBUFFER);

            SetWaterMark(true);

            LocateEngines();

            label2.Text = String.Format("IlMergeGui: v{0} {1}", Assembly.GetExecutingAssembly().GetName().Version,
                String.Format((registered ? "({0} extension registered)" : "({0} extension not registered, run elevated once)"), MyExtension));

            RestoreDefaults();

            //Mru Code
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(mruRegKey);
            if (regKey != null)
            {
                //clearMruRegistryOnExitMenuItem.Checked = (int)regKey.GetValue("delSubkey", 0) != 0;

                int delSub = (int)regKey.GetValue("delSubkey", 0);

                regKey.Close();
            }

            mruMenu = new MruStripMenuInline(fileToolStripMenuItem3, menuRecentFile, new MruStripMenu.ClickedHandler(OnMruFile), mruRegKey + "\\MRU", 16);
            mruMenu.LoadFromRegistry();

            menuStrip1.Update();
            menuStrip1.Refresh();

            AutoClose = false;
            ExitCode = 0;

            foreach (String arg2 in Environment.GetCommandLineArgs())
            {
                if (arg2.Equals("/close", StringComparison.OrdinalIgnoreCase))
                {
                    AutoClose = true;
                }
            }

            foreach (String arg in Environment.GetCommandLineArgs())
            {
                if (!arg.StartsWith("/") && arg.EndsWith(MyExtension, StringComparison.OrdinalIgnoreCase))
                {
                    if (File.Exists(arg))
                    {
                        RestoreSettings(arg);

                        foreach (String arg1 in Environment.GetCommandLineArgs())
                        {
                            if (arg1.Equals("/merge", StringComparison.OrdinalIgnoreCase))
                            {
                                btnMerge.PerformClick();
                            }
                        }

                        break;
                    }
                    else
                    {
                        ExitCode = 4;
                        ExitMsg = String.Format("Project File not Found:\r\n\r\n{0}", Path.GetFullPath(arg));

                        if (!AutoClose)
                        {
                            MessageBox.Show(ExitMsg, Application.ProductName);
                        }
                        else
                        {
                            ExitILMergeGUI();
                        }
                    }
                }
            }

            foreach (String arg0 in Environment.GetCommandLineArgs())
            {
                if (arg0.Equals("/?", StringComparison.OrdinalIgnoreCase) ||
                    arg0.Equals("/h?", StringComparison.OrdinalIgnoreCase) ||
                    arg0.Equals("/help", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Commandline syntax is:\r\n\r\n" +
                      String.Format("ILMergeGui <{0}> [/Merge] [/Close] [/?]\r\n\r\n", MyWildcard) +
                    " /Merge will automaticaly merge the supplied ilproject\r\n\r\n" +
                    " /Close will automaticaly close ILMergeGUI if /Merge is present\r\n\r\n" +
                    " /? will display this help\r\n", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Asterisk);

                    Environment.Exit(ExitCode);
                }
            }
        }

        private void ExitILMergeGUI()
        {
            Console.WriteLine(ExitMsg);
            Console.WriteLine("ExitCode: {0}", ExitCode);

            Environment.Exit(ExitCode);
        }

        private void Mainform_Shown(object sender, EventArgs e)
        {
            ListAssembly.Focus();
        }

        private void mnuFileExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void mnuFileNew_Click(object sender, EventArgs e)
        {
            this.Text = Application.ProductName;

            RestoreDefaults();
        }

        private void mnuFileOpen_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                RestoreSettings(openFileDialog1.FileName);

                mruMenu.AddFile(openFileDialog1.FileName);
                mruMenu.SetFirstFile(mruMenu.FindFilenameNumber(openFileDialog1.FileName));
                mruMenu.SaveToRegistry();

                saveFileDialog1.InitialDirectory = Path.GetDirectoryName(openFileDialog1.FileName);
                saveFileDialog1.FileName = Path.GetFileName(openFileDialog1.FileName);
            }
        }

        private void mnuFileSave_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                SaveSettings(saveFileDialog1.FileName);
            }
        }

        private void OnMruFile(int number, String filename)
        {
            if (File.Exists(filename))
            {
                RestoreSettings(filename);

                saveFileDialog1.InitialDirectory = Path.GetDirectoryName(filename);
                saveFileDialog1.FileName = Path.GetFileName(filename);

                mruMenu.SetFirstFile(number);
            }
            else
            {
                DialogResult result = MessageBox.Show(
                             "The file:\n\n'" + filename
                             + "'\n\ncannot be opened.\n\n"
                             + "Remove this file from the MRU list?"
                             , AppTitle
                             , MessageBoxButtons.YesNo);

                if (result == DialogResult.Yes)
                {
                    mruMenu.RemoveFile(number);
                }
            }

            mruMenu.SaveToRegistry();
        }

        /// <summary>
        /// Some sanity checks before merge.
        /// </summary>
        private void PreMerge()
        {
            if (String.IsNullOrEmpty(TxtKeyFile.Text))
            {
                ChkSignKeyFile.Checked = false;
            }

            if (String.IsNullOrEmpty(TxtLogFile.Text))
            {
                ChkGenerateLog.Checked = false;
            }

            if (TxtOutputAssembly.Text.Length < 5)
            {
                SelectOutputFile();
            }
        }

        /// <summary>
        /// Process selected or dropped Assemblies.
        /// </summary>
        /// <param name="filenames">The list of file to process.</param>
        private void ProcessFiles(String[] filenames)
        {
            UseWaitCursor = true;

            ListAssembly.BeginUpdate();

            Boolean isDupe = false;

            for (Int32 i = 0; i < filenames.Length; i++)
            {
                if (File.Exists(filenames[i]))
                {
                    Debug.WriteLine(filenames[i]);
                    String strExtension = Path.GetExtension(filenames[i]).ToLower().TrimStart('.');

                    if (Extensions.ContainsKey(strExtension))
                    {
                        isDupe = false;

                        for (Int32 z = 0; z < ListAssembly.Items.Count; z++)
                        {
                            if (filenames[i] == (String)ListAssembly.Items[z].Tag)
                            {
                                isDupe = true;
                            }
                        }

                        if (!isDupe)
                        {
                            ListViewItem lvi = ListAssembly.Items.Add(filenames[i]);

                            lvi.SubItems.Add(AssemblyName.GetAssemblyName(filenames[i]).Version.ToString());

                            lvi.Tag = filenames[i];
                            lvi.Group = ListAssembly.Groups[strExtension];
                        }
                    }
                }
                else if (Directory.Exists(filenames[i]))
                {
                    String[] files = Directory.GetFiles(filenames[i]);
                    ProcessFiles(files);

                    String[] directories = Directory.GetDirectories(filenames[i]);
                    ProcessFiles(directories);
                }
            }

            ListAssembly.Columns[0].Width = -1;
            ListAssembly.Columns[1].Width = -1;

            btnMerge.Enabled = ListAssembly.Items.Count > 1;

            if (String.IsNullOrEmpty(Primary))
            {
                if (ListAssembly.Groups[0].Items.Count > 0)
                {
                    ListAssembly.Groups[0].Items[0].Checked = true;
                }
            }

            UpdatePrimary();

            FormatItems();

            SetWaterMark(ListAssembly.Items.Count == 0);

            ListAssembly.EndUpdate();

            UseWaitCursor = false;
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (((RadioButton)sender).Checked)
            {
                LocateEngine(Merger.ILMerge);
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (((RadioButton)sender).Checked)
            {
                LocateEngine(Merger.ILRepack);
            }
        }

        private void RestoreDefaults()
        {
            ListAssembly.Items.Clear();
            Primary = String.Empty;
            LblPrimaryAssembly.Text = String.Empty;

            //Not allowed woth a DataSource.
            //CboTargetFramework.Items.Clear();

            frameworks = InstalledDotNetVersions();
            foreach (DotNet framework in frameworks)
            {
                Debug.WriteLine(String.Format("[{0}]", framework.name));
                Debug.WriteLine(String.Format("RegistryKey={0}", framework.key));
                Debug.WriteLine(String.Format("Version={0}", framework.version));
                if (Environment.Is64BitOperatingSystem)
                {
                    Debug.WriteLine(String.Format("x64 Windir Path={0}", framework.x64WindowsPath));
                    Debug.WriteLine(String.Format("x64 Program Files Path={0}", framework.x64ProgramFilesPath));
                }
                Debug.WriteLine(String.Format("x86 Windir Path={0}", framework.x86WindowsPath));
                Debug.WriteLine(String.Format("x86 Program Files Path={0}", framework.x86ProgramFilesPath));
                Debug.WriteLine("");
            }

            CboTargetFramework.DataSource = frameworks;
            CboTargetFramework.SelectedIndex = frameworks.Count - 1;

            CboDebug.SelectedIndex = 1;

            //ChkCopyAttributes.Checked = (Boolean)Settings.Default.Properties["CopyAttributes"].DefaultValue;
            ChkCopyAttributes.Checked = true;
            ChkUnionDuplicates.Checked = false;
            ChkSignKeyFile.Checked = false;
            ChkDelayedSign.Checked = false;

            //String x = Properties.Settings.Default["Log"].ToString();

            TxtKeyFile.Text = String.Empty;
            TxtLogFile.Text = String.Empty;
            TxtOutputAssembly.Text = String.Empty;
        }

        private void RestoreSettings(String filename)
        {
            XDocument doc = XDocument.Load(filename);

            this.Text = String.Format("{0} - [{1}]",
                Application.ProductName,
                Path.GetFileName(filename));

            //1) Restore Switches.
            ChkCopyAttributes.Checked = Boolean.Parse(doc.Root.Element("CopyAttributes").Attribute("Enabled").Value);
            ChkUnionDuplicates.Checked = Boolean.Parse(doc.Root.Element("UnionDuplicates").Attribute("Enabled").Value);
            CboDebug.SelectedIndex = Int32.Parse(doc.Root.Element("Debug").Attribute("Enabled").Value);
            if (doc.Root.Element("Internalize") != null)
            {
                ChkInternalize.Checked = Boolean.Parse(doc.Root.Element("Internalize").Attribute("Enabled").Value);
            }
            if (doc.Root.Element("MergeXml") != null)
            {
                ChkMergeXml.Checked = Boolean.Parse(doc.Root.Element("MergeXml").Attribute("Enabled").Value);
            }

            //2) Restore Signing.
            ChkSignKeyFile.Checked = Boolean.Parse(doc.Root.Element("Sign").Attribute("Enabled").Value);
            ChkDelayedSign.Checked = Boolean.Parse(doc.Root.Element("Sign").Attribute("Delay").Value);
            TxtKeyFile.Text = doc.Root.Element("Sign").Value;

            //3) Restore Logging.
            ChkGenerateLog.Checked = Boolean.Parse(doc.Root.Element("Log").Attribute("Enabled").Value);
            TxtLogFile.Text = doc.Root.Element("Log").Value;

            //4) Restore Assemblies.
            ListAssembly.Items.Clear();

            List<String> files = new List<String>();
            foreach (XElement assembly in doc.Root.Element("Assemblies").Elements("Assembly"))
            {
                files.Add(assembly.Value);
            }
            ProcessFiles(files.ToArray());

            Primary = doc.Root.Element("Assemblies").Element("Primary").Value;
            LblPrimaryAssembly.Text = Path.GetFileName(Primary);

            //5) Restore Output.
            TxtOutputAssembly.Text = doc.Root.Element("OutputAssembly").Value;

            //6) Restore Framework.
            String framework = doc.Root.Element("Framework").Value;
            foreach (Object o in CboTargetFramework.Items)
            {
                if (((DotNet)o).name.Equals(framework))
                {
                    CboTargetFramework.SelectedItem = o;
                    break;
                }
            }

            //7) Restore Engine
            if (doc.Root.Element("Engine") != null)
            {
                Engine = (Merger)Enum.Parse(typeof(Merger), doc.Root.Element("Engine").Attribute("Name").Value);

                LocateEngine(Engine);

                radioButton1.Checked = Engine == Merger.ILMerge;
                radioButton2.Checked = Engine == Merger.ILRepack;
            }
        }

        private void SaveSettings(String filename)
        {
            XDocument doc = new XDocument(new XElement("Settings"));

            //1) Save Switches.
            doc.Root.Add(
                new XComment("Switches"),
                new XElement("CopyAttributes", new XAttribute("Enabled", ChkCopyAttributes.Checked)),
                new XElement("UnionDuplicates", new XAttribute("Enabled", ChkUnionDuplicates.Checked)),
                new XElement("Debug", new XAttribute("Enabled", CboDebug.SelectedIndex)),
                new XElement("Internalize", new XAttribute("Enabled", ChkInternalize.Checked)),
                new XElement("MergeXml", new XAttribute("Enabled", ChkMergeXml.Checked)));

            //2) Save Signing.
            doc.Root.Add(
                new XComment("Signing"),
                    new XElement("Sign",
                    new XAttribute("Enabled", ChkSignKeyFile.Checked),
                    new XAttribute("Delay", ChkDelayedSign.Checked),
                    new XText(TxtKeyFile.Text)
                ));

            //3) Save Logging.
            doc.Root.Add(
                new XComment("Logging"),
                    new XElement("Log",
                    new XAttribute("Enabled", ChkGenerateLog.Checked),
                    new XText(TxtLogFile.Text)
                ));

            //4) Save Assemblies.
            XElement assemblies = new XElement("Assemblies");
            foreach (ListViewItem item in ListAssembly.Items)
            {
                assemblies.Add(new XElement("Assembly", (String)item.Tag));
            }
            assemblies.Add(
                new XElement("Primary", Primary));

            doc.Root.Add(
                new XComment("Assemblies"),
                assemblies);

            //5) Save Output.
            doc.Root.Add(
                new XComment("Output"),
                new XElement("OutputAssembly", TxtOutputAssembly.Text));

            //6) Save Framework.
            if (CboTargetFramework.SelectedIndex != -1)
            {
                DotNet framework = (DotNet)(CboTargetFramework.SelectedItem);
                doc.Root.Add(
                    new XComment(".NET Framework"),
                    new XElement("Framework", framework.name));
            }

            //7) Save Engine
            doc.Root.Add(
                new XComment("Merge Engine"),
                new XElement("Engine", new XAttribute("Name", Engine.ToString())));

            doc.Save(filename);

            this.Text = String.Format("{0} - [{1}]",
                Application.ProductName,
                Path.GetFileName(filename));
        }

        /// <summary>
        /// Select a Key File.
        /// </summary>
        private void SelectKeyFile()
        {
            SetOpenFileDefaults("Strong Name Key|*.snk");

            if (!String.IsNullOrEmpty(TxtKeyFile.Text))
            {
                openFile1.FileName = Path.GetFileName(TxtKeyFile.Text);
            }

            if (TxtKeyFile.Text.Length > 3)
            {
                openFile1.InitialDirectory = Path.GetDirectoryName(TxtKeyFile.Text);
            }

            if (openFile1.ShowDialog() == DialogResult.OK)
            {
                ChkSignKeyFile.Checked = true;
                TxtKeyFile.Text = openFile1.FileName;
                TxtKeyFile.Focus();
            }
        }

        /// <summary>
        /// Select a Log File.
        /// </summary>        
        private void SelectLogFile()
        {
            SetOpenFileDefaults("Log file|*.log");

            if (!String.IsNullOrEmpty(TxtLogFile.Text))
            {
                openFile1.FileName = Path.GetFileName(TxtLogFile.Text);
            }

            if (TxtLogFile.Text.Length > 3)
            {
                openFile1.InitialDirectory = Path.GetDirectoryName(TxtLogFile.Text);
            }

            if (openFile1.ShowDialog() == DialogResult.OK)
            {
                ChkGenerateLog.Checked = true;
                TxtLogFile.Text = openFile1.FileName;
                TxtLogFile.Focus();
            }
        }

        /// <summary>
        /// Select the Output File.
        /// </summary>        
        private void SelectOutputFile()
        {
            SetOpenFileDefaults("Assembly|*.dll;*.exe");

            if (ListAssembly.SelectedItems.Count != 0)
            {
                openFile1.FileName = Path.GetFileName((String)ListAssembly.SelectedItems[0].Tag);
            }

            if (TxtOutputAssembly.Text.Length > 3)
            {
                openFile1.InitialDirectory = Path.GetDirectoryName(TxtOutputAssembly.Text);
            }

            if (openFile1.ShowDialog() == DialogResult.OK)
            {
                TxtOutputAssembly.Text = openFile1.FileName;
                TxtOutputAssembly.Focus();
            }
        }

        /// <summary>
        /// Update the OpenFile Dialog before showing it.
        /// </summary>
        /// <param name="filter">Teh File Filter to set.</param>
        private void SetOpenFileDefaults(string filter)
        {
            openFile1.CheckFileExists = false;
            openFile1.Multiselect = false;
            openFile1.Filter = filter + "|All Files|*.*";
            openFile1.FileName = filter.Substring(filter.IndexOf('|') + 1);
        }

        private void SetWaterMark(Boolean show)
        {
            LVBKIMAGE backImage = new LVBKIMAGE();

            if (show)
            {
                backImage.ulFlags =
                    LVBKIF.STYLE_NORMAL |
                    LVBKIF.TYPE_WATERMARK |
                    LVBKIF.FLAG_ALPHABLEND;
                backImage.hbm = global::ILMergeGui.Properties.Resources.IconDropHere.GetHbitmap();
            }
            else
            {
                backImage.ulFlags = LVBKIF.SOURCE_NONE;
            }

            IntPtr pointer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(LVBKIMAGE)));
            Marshal.StructureToPtr(backImage, pointer, false);

            SendMessage(
                ListAssembly.Handle, (int)LVM.SETBKIMAGEW, IntPtr.Zero, pointer);

            //DefWndProc(ref message);

            Marshal.FreeHGlobal(pointer);

            ListAssembly.Invalidate();
        }

        private void UpdatePrimary()
        {
            btnMerge.Enabled = ListAssembly.Items.Count > 0;

            if (ListAssembly.CheckedItems != null && ListAssembly.CheckedItems.Count > 0)
            {
                Primary = (String)ListAssembly.CheckedItems[0].Tag;
                if (Path.GetFileName(Primary) != LblPrimaryAssembly.Text)
                {
                    LblPrimaryAssembly.Text = Path.GetFileName(Primary);
                }
            }
            else
            {
                Primary = String.Empty;
                if ("···" != LblPrimaryAssembly.Text)
                {
                    LblPrimaryAssembly.Text = "···";
                }
            }
        }

        private void visitWebsiteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClickOnceUpdater.VisitWebsite("http://ilmergegui.codeplex.com//");
        }

        /// <summary>
        /// The Background Worker Method, Starts the actual ILMerge.
        /// </summary>
        /// <param name="sender">-</param>
        /// <param name="e">-</param>
        private void WorkerILMerge_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            try
            {
                DynaInvoke.DynaClassInfo dci = DynaInvoke.GetClassReference(iLMergePath, ilMerge);

                Console.WriteLine("{0}.{0}()", ilMerge, "Merge");
                DynaInvoke.CallMethod(iLMergePath, ilMerge, "Merge", null);

                e.Result = null;
            }
            catch (Exception ex)
            {
                e.Result = ex;
            }
        }

        /// <summary>
        /// Called when the BackgroundWorker has completed it's task.
        /// </summary>
        /// <param name="sender">-</param>
        /// <param name="e">-</param>
        private void WorkerILMerge_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            //Cursor = Cursors.Default;

            EnableForm(true);

            ExitCode = 0;
            ExitMsg = String.Empty;

            if (e.Error != null)
            {
                //'ErrorHandler.Handle(e.Error)
            }

            if (e.Result != null)
            {
                //! The InnerException shows the true error from ILMerge (if present).
                String Message = (e.Result as Exception).InnerException == null ? (e.Result as Exception).Message : (e.Result as Exception).InnerException.Message;

                ExitCode = 1;
                ExitMsg = Resources.Error_MergeException + Environment.NewLine + Environment.NewLine + Message;

                if (!AutoClose)
                {
                    MessageBox.Show(ExitMsg, Resources.Error_Term, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else if (!File.Exists(TxtOutputAssembly.Text) || new FileInfo(TxtOutputAssembly.Text).Length == 0)
            {
                ExitCode = 2;
                ExitMsg = Resources.Error_CantMerge;

                if (!AutoClose)
                {
                    MessageBox.Show(ExitMsg, Resources.Error_Term, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                ExitMsg = Resources.AssembliesMerged;
                ExitCode = 0;

                if (!AutoClose)
                {
                    MessageBox.Show(ExitMsg, Resources.Done, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }

            if (!AutoClose && ChkGenerateLog.Checked && File.Exists(TxtLogFile.Text) && new FileInfo(TxtLogFile.Text).Length != 0)
            {
                Process.Start(new ProcessStartInfo(TxtLogFile.Text));
            }

            if (AutoClose)
            {
                ExitILMergeGUI();
            }
        }

        #endregion Methods

        #region Nested Types

        /// <summary>
        /// A structure to keep information on DotNet Frameworks.
        /// </summary>
        public struct DotNet
        {
            #region Fields

            /// <summary>
            /// The registry Key (ie SubVersion).
            /// </summary>
            public String key;

            /// <summary>
            /// The Friendly Name.
            /// </summary>
            public String name;

            /// <summary>
            /// The Version.
            /// </summary>
            public Version version;

            /// <summary>
            /// The Path under C:\Program Files\Reference Assemblies.
            /// </summary>
            public String x64ProgramFilesPath;

            /// <summary>
            /// The Path under C:\Windows\Microsoft.NET\Framework64
            /// </summary>
            public String x64WindowsPath;

            /// <summary>
            /// The Path under C:\Program Files (x86)\Reference Assemblies.
            /// </summary>            
            public String x86ProgramFilesPath;

            /// <summary>
            /// The Path under C:\Windows\Microsoft.NET\Framework
            /// </summary>
            public String x86WindowsPath;

            #endregion Fields

            #region Methods

            /// <summary>
            /// Genenerate a Friendly (shorter) ToString().
            /// </summary>
            /// <returns></returns>
            public override String ToString()
            {
                return name;
            }

            #endregion Methods
        }

        /// <summary>
        /// http://social.msdn.microsoft.com/forums/en-US/winforms/thread/86d8a8bf-8bc0-4567-970b-19a96b0e9b7c/
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct LVBKIMAGE
        {
            public LVBKIF ulFlags;
            public IntPtr hbm;
            public IntPtr pszImage;
            public int cchImageMax;
            public int xOffsetPercent;
            public int yOffsetPercent;
        }

        #endregion Nested Types
    }
}
