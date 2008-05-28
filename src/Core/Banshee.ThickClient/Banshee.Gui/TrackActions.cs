//
// TrackActions.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using Mono.Unix;
using Gtk;

using Banshee.Base;
using Banshee.Query;
using Banshee.Sources;
using Banshee.Library;
using Banshee.Playlist;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.ServiceStack;
using Banshee.Widgets;
using Banshee.Gui.Dialogs;
using Banshee.Gui.Widgets;

namespace Banshee.Gui
{
    public class TrackActions : BansheeActionGroup
    {
        private RatingActionProxy rating_proxy;

        private static readonly string [] require_selection_actions = new string [] {
            "TrackContextMenuAction", "TrackPropertiesAction", "AddToPlaylistAction",
            "RemoveTracksAction", "RemoveTracksFromLibraryAction", "DeleteTracksFromDriveAction",
            "RateTracksAction", "SelectNoneAction"
        };
        
        public event EventHandler SelectionChanged;

        public TrackActions (InterfaceActionService actionService) : base (actionService, "Track")
        {
            Add (new ActionEntry [] {
                new ActionEntry("TrackContextMenuAction", null, 
                    String.Empty, null, null, OnTrackContextMenu),

                new ActionEntry("SelectAllAction", null,
                    Catalog.GetString("Select _All"), "<control>A",
                    Catalog.GetString("Select all tracks"), OnSelectAll),
                    
                new ActionEntry("SelectNoneAction", null,
                    Catalog.GetString("Select _None"), "<control><shift>A",
                    Catalog.GetString("Unselect all tracks"), OnSelectNone),

                new ActionEntry ("TrackPropertiesAction", Stock.Edit,
                    Catalog.GetString ("_Edit Track Information"), "E",
                    Catalog.GetString ("Edit information on selected tracks"), OnTrackProperties),

                new ActionEntry ("AddToPlaylistAction", null,
                    Catalog.GetString ("Add _to Playlist"), null,
                    Catalog.GetString ("Append selected items to playlist or create new playlist from selection"),
                    OnAddToPlaylistMenu),

                new ActionEntry ("AddToNewPlaylistAction", Stock.New,
                    Catalog.GetString ("New Playlist"), null,
                    Catalog.GetString ("Create new playlist from selected tracks"),
                    OnAddToNewPlaylist),

                new ActionEntry ("RemoveTracksAction", Stock.Remove,
                    Catalog.GetString("_Remove"), "Delete",
                    Catalog.GetString("Remove selected track(s) from this source"), OnRemoveTracks),

                new ActionEntry ("RemoveTracksFromLibraryAction", null,
                    Catalog.GetString("Remove From _Library"), "",
                    Catalog.GetString("Remove selected track(s) from library"), OnRemoveTracksFromLibrary),

                new ActionEntry ("DeleteTracksFromDriveAction", null,
                    Catalog.GetString ("_Delete From Drive"), null,
                    Catalog.GetString ("Permanently delete selected item(s) from medium"), OnDeleteTracksFromDrive),

                new ActionEntry ("RateTracksAction", null,
                    String.Empty, null, null, OnRateTracks),

                new ActionEntry ("SearchMenuAction", Stock.Find,
                    Catalog.GetString ("_Search"), null,
                    Catalog.GetString ("Search for items matching certain criteria"), null),

                new ActionEntry ("SearchForSameAlbumAction", null,
                    Catalog.GetString ("By Matching _Album"), null,
                    Catalog.GetString ("Search all songs of this album"), OnSearchForSameAlbum),

                new ActionEntry ("SearchForSameArtistAction", null,
                    Catalog.GetString ("By Matching A_rtist"), null,
                    Catalog.GetString ("Search all songs of this artist"), OnSearchForSameArtist),

                //new ActionEntry ("JumpToPlayingTrackAction", null,
                //    Catalog.GetString ("_Jump to playing song"), "<control>J",
                //    null, OnJumpToPlayingTrack),
            });

            Actions.UIManager.ActionsChanged += HandleActionsChanged;

            Actions.GlobalActions["EditMenuAction"].Activated += HandleEditMenuActivated;
            ServiceManager.SourceManager.ActiveSourceChanged += HandleActiveSourceChanged;

            this["AddToPlaylistAction"].HideIfEmpty = false;
        }

#region State Event Handlers

        private ITrackModelSource current_source;
        private void HandleActiveSourceChanged (SourceEventArgs args)
        {
            if (current_source != null && current_source.TrackModel != null) {
                current_source.TrackModel.Selection.Changed -= HandleSelectionChanged;
                current_source = null;
            }
            
            ITrackModelSource new_source = ActiveSource as ITrackModelSource;
            if (new_source != null) {
                new_source.TrackModel.Selection.Changed += HandleSelectionChanged;
                current_source = new_source;
            }
            
            UpdateActions ();
        }

        private void HandleActionsChanged (object sender, EventArgs args)
        {
            if (Actions.UIManager.GetAction ("/MainMenu/EditMenu") != null) {
                rating_proxy = new RatingActionProxy (Actions.UIManager, this["RateTracksAction"]);
                rating_proxy.AddPath ("/MainMenu/EditMenu", "AddToPlaylist");
                rating_proxy.AddPath ("/TrackContextMenu", "AddToPlaylist");
                Actions.UIManager.ActionsChanged -= HandleActionsChanged;
            }
        }

        private void HandleSelectionChanged (object sender, EventArgs args)
        {
            OnSelectionChanged ();
            UpdateActions ();
        }

        private void HandleEditMenuActivated (object sender, EventArgs args)
        {
            ResetRating ();
        }
        
        private void OnSelectionChanged ()
        {
            EventHandler handler = SelectionChanged;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

#endregion

#region Utility Methods

        private bool select_actions_suppressed = false;
        public void SuppressSelectActions ()
        {
            if (!select_actions_suppressed) {
                this ["SelectAllAction"].DisconnectAccelerator ();
                this ["SelectNoneAction"].DisconnectAccelerator ();
                select_actions_suppressed = true;
            }
        }

        public void UnsuppressSelectActions ()
        {
            if (select_actions_suppressed) {
                this ["SelectAllAction"].ConnectAccelerator ();
                this ["SelectNoneAction"].ConnectAccelerator ();
                select_actions_suppressed = false;
            }
        }

        private void UpdateActions ()
        {
            Source source = ServiceManager.SourceManager.ActiveSource;
            bool in_database = source is DatabaseSource;
            
            Hyena.Collections.Selection selection = (source is ITrackModelSource) ? (source as ITrackModelSource).TrackModel.Selection : null;

            if (selection != null) {
                bool has_selection = selection.Count > 0;
                foreach (string action in require_selection_actions)
                    this[action].Sensitive = has_selection;

                bool has_single_selection = selection.Count == 1;

                this["SelectAllAction"].Sensitive = !selection.AllSelected;

                if (source != null) {
                    ITrackModelSource track_source = source as ITrackModelSource;
                    bool is_track_source = track_source != null;

                    UpdateActions (is_track_source && source.CanSearch, has_single_selection,
                       "SearchMenuAction", "SearchForSameArtistAction", "SearchForSameAlbumAction"
                    );

                    UpdateAction ("RemoveTracksAction", is_track_source && track_source.CanRemoveTracks, has_selection, source);
                    UpdateAction ("DeleteTracksFromDriveAction", is_track_source && track_source.CanDeleteTracks, has_selection, source);
                    UpdateAction ("RemoveTracksFromLibraryAction", source.Parent is LibrarySource, has_selection, null);
                    
                    UpdateAction ("TrackPropertiesAction", in_database, has_selection, null);
                    UpdateAction ("RateTracksAction", in_database, has_selection, null);
                    UpdateAction ("AddToPlaylistAction", in_database, has_selection, null);
                }
            }
        }

        private void ResetRating ()
        {
            if (current_source != null) {
                int rating = 0;
    
                // If there is only one track, get the preset rating
                if (current_source.TrackModel.Selection.Count == 1) {
                    foreach (TrackInfo track in current_source.TrackModel.SelectedItems) {
                        rating = track.Rating;
                    }
                }
                rating_proxy.Reset (rating);
            }
        }

#endregion
            
#region Action Handlers

        private void OnSelectAll (object o, EventArgs args)
        {
            if (current_source != null)
                current_source.TrackModel.Selection.SelectAll ();
        }

        private void OnSelectNone (object o, EventArgs args)
        {
            if (current_source != null)
                current_source.TrackModel.Selection.Clear ();
        }

        private void OnTrackContextMenu (object o, EventArgs args)
        {
            ResetRating ();
            ShowContextMenu ("/TrackContextMenu");
        }

        private void OnTrackProperties (object o, EventArgs args)
        {
            if (current_source != null) {
                TrackEditor propEdit = new TrackEditor (current_source.TrackModel.SelectedItems);
                propEdit.Saved += delegate {
                    //ui.playlistView.QueueDraw();
                };
            }
        }

        // Called when the Add to Playlist action is highlighted.
        // Generates the menu of playlists to which you can add the selected tracks.
        private void OnAddToPlaylistMenu (object o, EventArgs args)
        {
            Source active_source = ServiceManager.SourceManager.ActiveSource;

            // TODO find just the menu that was activated instead of modifying all proxies
            foreach (Widget proxy_widget in (o as Gtk.Action).Proxies) {
                MenuItem menu = proxy_widget as MenuItem;
                if (menu == null)
                    continue;

                Menu submenu = new Menu ();
                menu.Submenu = submenu;

                submenu.Append (this ["AddToNewPlaylistAction"].CreateMenuItem ());
                bool separator_added = false;
                
                foreach (Source child in ActivePrimarySource.Children) {
                    PlaylistSource playlist = child as PlaylistSource;
                    if (playlist != null) {
                        if (!separator_added) {
                            submenu.Append (new SeparatorMenuItem ());
                            separator_added = true;
                        }
                        
                        PlaylistMenuItem item = new PlaylistMenuItem (playlist);
                        item.Image = new Gtk.Image ("playlist-source", IconSize.Menu);
                        item.Activated += OnAddToExistingPlaylist;
                        item.Sensitive = playlist != active_source;
                        submenu.Append (item);
                    }
                }
                
                submenu.ShowAll ();
            }
        }

        private void OnAddToNewPlaylist (object o, EventArgs args)
        {
            // TODO generate name based on the track selection, or begin editing it
            PlaylistSource playlist = new PlaylistSource ("New Playlist", ActivePrimarySource.DbId);
            playlist.Save ();
            playlist.PrimarySource.AddChildSource (playlist);
            ThreadAssist.SpawnFromMain (delegate {
                playlist.AddSelectedTracks (ActiveSource);
            });
        }

        private void OnAddToExistingPlaylist (object o, EventArgs args)
        {
            ThreadAssist.SpawnFromMain (delegate {
                ((PlaylistMenuItem)o).Playlist.AddSelectedTracks (ActiveSource);
            });
        }

        private void OnRemoveTracks (object o, EventArgs args)
        {
            ITrackModelSource source = ActiveSource as ITrackModelSource;

            if (!ConfirmRemove (source, false, source.TrackModel.Selection.Count))
                return;

            if (source != null && source.CanRemoveTracks) {
                ThreadAssist.SpawnFromMain (delegate {
                    source.RemoveSelectedTracks ();
                });
            }
        }

        private void OnRemoveTracksFromLibrary (object o, EventArgs args)
        {
            ITrackModelSource source = ActiveSource as ITrackModelSource;

            if (source != null) {
                LibrarySource library = source.Parent as LibrarySource;
                if (library != null) {
                    if (!ConfirmRemove (library, false, source.TrackModel.Selection.Count)) {
                        return;
                    }

                    ThreadAssist.SpawnFromMain (delegate {
                        library.RemoveSelectedTracks (source.TrackModel as DatabaseTrackListModel);
                    });
                }
            }
        }

        private void OnDeleteTracksFromDrive (object o, EventArgs args)
        {
            ITrackModelSource source = ActiveSource as ITrackModelSource;

            if (!ConfirmRemove (source, true, source.TrackModel.Selection.Count))
                return;

            if (source != null && source.CanDeleteTracks) {
                source.DeleteSelectedTracks ();
            }
        }

        private void OnRateTracks (object o, EventArgs args)
        {
            ThreadAssist.SpawnFromMain (delegate {
                (ActiveSource as DatabaseSource).RateSelectedTracks (rating_proxy.LastRating);
            });
        }

        private void OnSearchForSameArtist (object o, EventArgs args)
        {
            if (current_source != null) {
                foreach (TrackInfo track in current_source.TrackModel.SelectedItems) {
                    ActiveSource.FilterQuery = BansheeQuery.ArtistField.ToTermString (":", track.ArtistName);
                    break;
                }
            }
        }

        private void OnSearchForSameAlbum (object o, EventArgs args)
        {
            if (current_source != null) {
                foreach (TrackInfo track in current_source.TrackModel.SelectedItems) {
                    ActiveSource.FilterQuery = BansheeQuery.AlbumField.ToTermString (":", track.AlbumTitle);
                    break;
                }
            }
        }

        /*private void OnJumpToPlayingTrack (object o, EventArgs args)
        {
        }*/

#endregion

        private static bool ConfirmRemove (ITrackModelSource source, bool delete, int selCount)
        {
            if (!source.ConfirmRemoveTracks) {
                return true;
            }
            
            bool ret = false;
            string header = null;
            string message = null;
            string button_label = null;
            
            if (delete) {
                header = String.Format (
                    Catalog.GetPluralString (
                        "Are you sure you want to permanently delete this item?",
                        "Are you sure you want to permanently delete the selected {0} items?", selCount
                    ), selCount
                );
                message = Catalog.GetString ("If you delete the selection, it will be permanently lost.");
                button_label = "gtk-delete";
            } else {
                header = String.Format (Catalog.GetString ("Remove selection from {0}?"), source.Name);
                message = String.Format (
                    Catalog.GetPluralString (
                        "Are you sure you want to remove the selected item from your {1}?",
                        "Are you sure you want to remove the selected {0} items from your {1}?", selCount
                    ), selCount, source.GenericName
                );
                button_label = "gtk-remove";
            }
                
            HigMessageDialog md = new HigMessageDialog (
                ServiceManager.Get<GtkElementsService> ("GtkElementsService").PrimaryWindow,
                DialogFlags.DestroyWithParent, delete ? MessageType.Warning : MessageType.Question,
                ButtonsType.None, header, message
            );
            md.AddButton ("gtk-cancel", ResponseType.No, false);
            md.AddButton (button_label, ResponseType.Yes, false);
            
            try {
                if (md.Run () == (int) ResponseType.Yes) {
                    ret = true;
                }
            } finally {
                md.Destroy ();
            }
            return ret;
        }
    }
}
