using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.TextManager.Interop;

namespace CollapseRegionExtension
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class CollapseRegionExtensionCommands
    {
        #region Menu command IDs

        /// <summary>
        /// Expand All Command ID.
        /// </summary>
        public const int ExpandAllCommandId = 0x0101;

        /// <summary>
        /// Collapse All Command ID.
        /// </summary>
        public const int CollapseAllCommandId = 0x0102;

        /// <summary>
        /// Expand Current Command ID.
        /// </summary>
        public const int ExpandCurrentCommandId = 0x0103;

        /// <summary>
        /// Collapse Current Command ID.
        /// </summary>
        public const int CollapseCurrentCommandId = 0x0104;

        /// <summary>
        /// Expand all but current Command ID.
        /// </summary>
        public const int ExpandButCurrentCommandId = 0x0105;

        /// <summary>
        /// Collapse all but current Command ID.
        /// </summary>
        public const int CollapseButCurrentCommandId = 0x0106;

        /// <summary>
        /// Expand all xml-comments with summary Command ID.
        /// </summary>
        public const int ExpandAllSummaryCommandId = 0x0107;

        /// <summary>
        /// Collapse all xml-comments with summary Command ID.
        /// </summary>
        public const int CollapseAllSummaryCommandId = 0x0108;

        #region Internal for testing 1
        // Internal for testing
        #endregion

        #region Internal for testing 2
        // Internal for testing
        #endregion

        #endregion

        #region External for testing 3
        // Internal for testing
        #endregion

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid( "6116681c-345c-4371-8a03-98c18c8e9479" );

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage packageInstance;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollapseRegionExtensionCommands"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private CollapseRegionExtensionCommands( AsyncPackage package )
        {
            this.packageInstance = package ?? throw new ArgumentNullException( nameof( package ) );
        }

        private void Init( OleMenuCommandService commandService )
        {
            foreach( var id in new int[]
            {
                ExpandAllCommandId, CollapseAllCommandId,
                ExpandCurrentCommandId, CollapseCurrentCommandId,
                ExpandButCurrentCommandId, CollapseButCurrentCommandId,
                ExpandAllSummaryCommandId, CollapseAllSummaryCommandId
            } )
            {
                var commandId = new CommandID( CommandSet, id );
                var menuItem = new MenuCommand( CollapseExpand, commandId );
                commandService.AddCommand( menuItem );
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static CollapseRegionExtensionCommands Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IAsyncServiceProvider ServiceProvider => packageInstance;

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="packageInstance">Owner package, not null.</param>
        public static async Task InitializeAsync( AsyncPackage packageInstance )
        {
            if( packageInstance == null )
                throw new ArgumentNullException( nameof( packageInstance ) );

            if( ThreadHelper.JoinableTaskFactory == null )
            {
                System.Diagnostics.Debug.WriteLine( $"CollapseRegionExtension failed: " +
                    $"ThreadHelper.JoinableTaskFactory == {( ThreadHelper.JoinableTaskFactory == null ? "null" : "not null" )}, " +
                    $"packageInstance.JoinableTaskFactory == {( packageInstance.JoinableTaskFactory == null ? "null" : "not null" )}" );
                throw new NullReferenceException( $"CollapseRegionExtension failed: " +
                    $"ThreadHelper.JoinableTaskFactory == {( ThreadHelper.JoinableTaskFactory == null ? "null" : "not null" )}, " +
                    $"packageInstance.JoinableTaskFactory == {( packageInstance.JoinableTaskFactory == null ? "null" : "not null" )}" );
            }
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync( packageInstance.DisposalToken );
            Instance = new CollapseRegionExtensionCommands( packageInstance );
            var commandService = await packageInstance.GetServiceAsync( ( typeof( IMenuCommandService ) ) ) as OleMenuCommandService;
            if( commandService == null )
            {
                System.Diagnostics.Debug.WriteLine( "CollapseRegionExtension failed to retrieve OleMenuCommandService service" );
                throw new NullReferenceException( "CollapseRegionExtension failed to retrieve OleMenuCommandService service" );
            }
            Instance.Init( commandService );
        }

        private void CollapseExpand( object sender, EventArgs e )
        {
            if( packageInstance == null )
            {
                System.Diagnostics.Debug.WriteLine( "CollapseRegionExtension failed: packageInstance == null" );
                throw new NullReferenceException( "CollapseRegionExtension failed: packageInstance == null" );
            }
            if( sender == null )
                throw new ArgumentNullException( nameof( sender ) );

            //ThreadHelper.ThrowIfNotOnUIThread();

            int id = ( sender as MenuCommand ).CommandID.ID;
            bool expand = id == ExpandAllCommandId || id == ExpandCurrentCommandId || id == ExpandButCurrentCommandId ||
                id == ExpandAllSummaryCommandId;
            bool current = id == ExpandCurrentCommandId || id == CollapseCurrentCommandId;
            bool butCurrent = id == ExpandButCurrentCommandId || id == CollapseButCurrentCommandId;
            bool comment = id == ExpandAllSummaryCommandId || id == CollapseAllSummaryCommandId;

            var compareInfo = CultureInfo.CurrentCulture.CompareInfo;
            Stack<Tuple<int, int>> stack = current || butCurrent ? new Stack<Tuple<int, int>>() : null;

            _ = Task.Run( async () =>
            {
                IComponentModel componentModel = await AsyncServiceProvider.GlobalProvider
                    .GetServiceAsync( typeof( SComponentModel ) ) as IComponentModel;

                if( componentModel == null )
                    return;

                IOutliningManagerService outliningManagerService = componentModel.GetService<IOutliningManagerService>();
                if( outliningManagerService == null )
                    return;

                var editorAdaptersFactoryService = componentModel.GetService<IVsEditorAdaptersFactoryService>();
                if( editorAdaptersFactoryService == null )
                    return;

                if( !( await ServiceProvider.GetServiceAsync( typeof( SVsTextManager ) ) is IVsTextManager textManager ) )
                    return;

                textManager.GetActiveView( 1, null, out IVsTextView textViewAdapter );
                if( textViewAdapter == null )
                    return;

                var textView = editorAdaptersFactoryService.GetWpfTextView( textViewAdapter );
                var snapshot = textView.TextSnapshot;
                var snapshotSpan = new SnapshotSpan( snapshot, new Span( 0, snapshot.Length ) );
                var outliningManager = outliningManagerService.GetOutliningManager( textView );
                if( outliningManager == null )
                    return;

                if( ThreadHelper.JoinableTaskFactory == null && packageInstance.JoinableTaskFactory == null )
                {
                    System.Diagnostics.Debug.WriteLine( $"CollapseRegionExtension failed: " +
                        $"ThreadHelper.JoinableTaskFactory == {( ThreadHelper.JoinableTaskFactory == null ? "null" : "not null" )}, " +
                        $"packageInstance.JoinableTaskFactory == {( packageInstance.JoinableTaskFactory == null ? "null" : "not null" )}" );
                    throw new NullReferenceException( $"CollapseRegionExtension failed: " +
                        $"ThreadHelper.JoinableTaskFactory == {( ThreadHelper.JoinableTaskFactory == null ? "null" : "not null" )}, " +
                        $"packageInstance.JoinableTaskFactory == {( packageInstance.JoinableTaskFactory == null ? "null" : "not null" )}" );
                }
                // VS extentions sometimes have bugs ;-)
                await ( packageInstance.JoinableTaskFactory ?? ThreadHelper.JoinableTaskFactory ).SwitchToMainThreadAsync();

                var caret = textView.Caret.Position.BufferPosition;
                var caretLineNumber = caret.GetContainingLineNumber();

                if( !comment && ( current || butCurrent ) )
                {
                    // Enumerate all regions to look deepest region caret is in
                    IEnumerable<ICollapsible> regions = outliningManager.GetAllRegions( snapshotSpan );
                    foreach( var region in regions )
                    {
                        int firstLineNumber = region.Extent.GetSpan( textView.TextSnapshot ).Start.GetContainingLineNumber();
                        int lastLineNumber = region.Extent.GetSpan( textView.TextSnapshot ).End.GetContainingLineNumber();

                        if( caretLineNumber < firstLineNumber || caretLineNumber > lastLineNumber )
                            continue;

                        var text = region.Extent.GetText( snapshot );
                        bool isRegion =
                            text.StartsWith( "#region", StringComparison.OrdinalIgnoreCase ) ||
                            text.StartsWith( "#pragma region", StringComparison.OrdinalIgnoreCase ) ||
                            text.StartsWith( "<!--", StringComparison.OrdinalIgnoreCase ) &&
                                compareInfo.IndexOf( text, "region", CompareOptions.OrdinalIgnoreCase ) >= 0;

                        if( !isRegion )
                            continue;
                        if( butCurrent ||
                            ( expand && region.IsCollapsed || !expand && !region.IsCollapsed ) )
                            stack.Push( new Tuple<int, int>( firstLineNumber, lastLineNumber ) );
                    }
                }

                Tuple<int, int>[] checkArray = stack?.ToArray() ?? Array.Empty<Tuple<int, int>>();

                Predicate<ICollapsible> predicate = new Predicate<ICollapsible>( (/*ICollapsible*/ match ) =>
                {
                    int firstLineNumber = match.Extent.GetSpan( textView.TextSnapshot ).Start.GetContainingLineNumber();
                    int lastLineNumber = match.Extent.GetSpan( textView.TextSnapshot ).End.GetContainingLineNumber();

                    // Check of caret inside or outside of current region by line numbers.
                    // Because when #region is collapsed, Contains(caret) returns false.

                    if( !comment )
                    {
                        if( current && ( caretLineNumber < firstLineNumber || lastLineNumber < caretLineNumber ) ||
                            butCurrent && ( firstLineNumber <= caretLineNumber && caretLineNumber <= lastLineNumber ) )
                            return false;
                    }

                    var text = match.Extent.GetText( snapshot );
                    bool isRegion =
                        text.StartsWith( "#region", StringComparison.OrdinalIgnoreCase ) ||
                        text.StartsWith( "#pragma region", StringComparison.OrdinalIgnoreCase ) ||
                        text.StartsWith( "<!--", StringComparison.OrdinalIgnoreCase ) &&
                            compareInfo.IndexOf( text, "region", CompareOptions.OrdinalIgnoreCase ) >= 0;

                    if( !isRegion )
                    {
                        if( !comment )
                            return false;

                        if( text.StartsWith( "///", StringComparison.OrdinalIgnoreCase ) )
                        {
                            for( int index = 3; index < text.Length; index++ )
                            {
                                if( text[ index ] == ' ' )
                                    continue;
                                if( text.Substring( index ).StartsWith( "<summary>", StringComparison.OrdinalIgnoreCase ) )
                                    return true;
                                return false;
                            }
                        }
                        return false;
                    }

                    if( checkArray.Length == 0 )
                        return true;

                    if( butCurrent )
                        return checkArray.Length == 0 ||
                            ( checkArray[ 0 ].Item2 < firstLineNumber || lastLineNumber < checkArray[ 0 ].Item1 );

                    if( current )
                    {
                        bool found = false;
                        for( int index = 0; index < checkArray.Length; index++ )
                        {
                            if( checkArray[ index ].Item1 == firstLineNumber &&
                                checkArray[ index ].Item2 == lastLineNumber )
                            {
                                found = true;
                                if( index == 0 )
                                    return true;
                            }
                        }
                        if( found )
                            return false;
                        else
                            return true;
                    }

                    return true;
                } );

                if( expand )
                {
                    outliningManager.ExpandAll( snapshotSpan, predicate );
                }
                else
                {
                    outliningManager.CollapseAll( snapshotSpan, predicate );
                }
            } );
        }
    }
}
