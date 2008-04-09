//
// ConnectedRepeatComboBox.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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
using Gtk;

using Hyena.Widgets;

using Banshee.ServiceStack;

namespace Banshee.Gui.Widgets
{
    public class RepeatActionButton : HBox
    {
        private PlaybackRepeatActions actions = ServiceManager.Get<InterfaceActionService> ().PlaybackActions.RepeatActions;
        
        private MenuButton button;
        private HBox box = new HBox ();
        private Image image = new Image ();
        private Label label = new Label ();
        
        public RepeatActionButton ()
        {
            box.Spacing = 4;
            label.UseUnderline = true;
            image.IconSize = (int)IconSize.Menu;
            
            actions.Changed += delegate { OnActionChanged (); };
            OnActionChanged ();
            
            box.PackStart (image, false, false, 0);
            box.PackStart (label, true, true, 0);
            button = new MenuButton (box, actions.CreateMenu (), false);
            Add (button);
            ShowAll ();
        }
        
        private void OnActionChanged ()
        {
            image.IconName = actions.Active.IconName;
            label.TextWithMnemonic = actions.Active.Label;
        }
    }
}
