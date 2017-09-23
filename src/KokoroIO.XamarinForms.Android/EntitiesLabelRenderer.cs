﻿using System;
using System.Linq;
using Android.Text;
using Android.Text.Method;
using Android.Text.Style;
using Android.Views;
using KokoroIO.XamarinForms.Droid;
using KokoroIO.XamarinForms.ViewModels;
using KokoroIO.XamarinForms.Views;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(EntitiesLabel), typeof(EntitiesLabelRenderer))]

namespace KokoroIO.XamarinForms.Droid
{
    public sealed class EntitiesLabelRenderer : LabelRenderer
    {
        private class EntitySpan : ClickableSpan
        {
            private readonly EntitiesLabelRenderer _Renderer;
            private readonly object _Entity;
            public EntitySpan(EntitiesLabelRenderer r, object entity)
            {
                _Renderer = r;
                _Entity = entity;
            }

            public override void OnClick(Android.Views.View widget)
                => _Renderer.OnClick(_Entity);
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Label> e)
        {
            base.OnElementChanged(e);

            if (Element is EntitiesLabel ml)
            {
                ml.TextUpdater = UpdateEntitiesText;
            }

            if (Control != null)
            {
                Control.SetMaxLines(1);
                Control.Ellipsize = TextUtils.TruncateAt.End;
            }
        }

        private void UpdateEntitiesText()
        {
            if (Control == null)
            {
                return;
            }

            if (Element is EntitiesLabel ml)
            {
                if (ml.Entities != null)
                {
                    var ssb = new SpannableStringBuilder();
                    foreach (var m in ml.Entities)
                    {
                        if (ssb.Length() > 0)
                        {
                            ssb.Append(' ');
                        }

                        var i = ssb.Length();

                        ssb.Append(ml.GetText(m));

                        ssb.SetSpan(new EntitySpan(this, m), i, ssb.Length(), SpanTypes.ExclusiveExclusive);
                    }

                    if (ssb.Length() > 0)
                    {
                        Control.TextFormatted = ssb;
                        Control.MovementMethod = LinkMovementMethod.Instance;
                        return;
                    }
                }
            }

            Control.Text = null;
        }

        private void OnClick(object mb)
        {
            var c = (Element as EntitiesLabel)?.SelectCommand;

            if (c != null)
            {
                c.Execute(mb);
            }
        }
    }
}