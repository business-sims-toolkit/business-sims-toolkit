using System;
using System.Drawing;
using System.Windows.Forms;

using Algorithms;
using Events;

// ReSharper disable PossibleInvalidOperationException
// ReSharper disable ParameterHidesMember

namespace ResizingUi.Component
{
    public struct AnimationProperties
    {
        public PointF? Location { get; set; }
        public SizeF? Size { get; set; }
        public Color? Colour { get; set; }

        public bool DiffersWhereNonNull(AnimationProperties that)
        {
            if (Location.HasValue && that.Location.HasValue && (Location != that.Location))
            {
                return true;
            }

            if (Size.HasValue && that.Size.HasValue && (Size != that.Size))
            {
                return true;
            }

            if (Colour.HasValue && that.Colour.HasValue && (!Colour.Value.EqualsByComponents(that.Colour.Value)))
            {
                return true;
            }

            return false;
        }
    }

    public class ControlAnimationComponent
    {
        readonly Timer timer;

        const float millisecondsInSeconds = 1000;
        const int timerInterval = 20;

        float elapsedTime;
        float fractionThroughAnimation;
        float animationDuration;
        
        AnimationProperties targetProperties;
        AnimationProperties currentProperties;
        AnimationProperties startingProperties;

        //public event EventHandler<AnimationPropertiesEventArgs> AnimationTick;

	    public event EventHandler<EventArgs<AnimationProperties>> AnimationTick;

        public ControlAnimationComponent()
        {
            timer = new Timer
            {
                Interval = timerInterval
            };
            timer.Stop();
            timer.Tick += timer_Tick;
        }

        public void AnimateTo(AnimationProperties startingProperties, AnimationProperties targetProperties, float animationDurationInSeconds)
        {
            // If we're already animating towards the same target, don't start again.
            if (timer.Enabled
                && (!targetProperties.DiffersWhereNonNull(this.targetProperties)))
            {
                return;
            }

            this.startingProperties = startingProperties;
            this.targetProperties = new AnimationProperties
            {
                Location = targetProperties.Location ?? startingProperties.Location,
                Size = targetProperties.Size ?? startingProperties.Size,
                Colour = targetProperties.Colour ?? startingProperties.Colour
            };

            animationDuration = animationDurationInSeconds * millisecondsInSeconds;
            elapsedTime = 0f;

            timer.Start();
        }

        void OnAnimationTick()
        {
            AnimationTick?.Invoke(this, AnimationTick.CreateArgs(currentProperties));
        }

        void timer_Tick(object sender, EventArgs e)
        {
            elapsedTime += timerInterval;
            fractionThroughAnimation = Math.Min(elapsedTime / animationDuration, 1);

            var step = Maths.SmoothStep(fractionThroughAnimation);


            if (startingProperties.Location.HasValue && startingProperties.Size.HasValue)
            {
                var bounds = Maths.Lerp(step,
                    new RectangleF(startingProperties.Location.Value, startingProperties.Size.Value),
                    new RectangleF(targetProperties.Location.Value, targetProperties.Size.Value));

                currentProperties.Location = bounds.Location;
                currentProperties.Size = bounds.Size;
            }
            else if (startingProperties.Location.HasValue)
            {
                currentProperties.Location = Maths.Lerp(step, startingProperties.Location.Value, targetProperties.Location.Value);
            }
            else if (startingProperties.Size.HasValue)
            {
                currentProperties.Size = Maths.Lerp(step, startingProperties.Size.Value, targetProperties.Size.Value);
            }

            if (startingProperties.Colour.HasValue)
            {
                currentProperties.Colour =
                    Maths.Lerp(step, startingProperties.Colour.Value, targetProperties.Colour.Value);
            }

            if (fractionThroughAnimation >= 1)
            {
                timer.Stop();
            }

            OnAnimationTick();
        }
    }
}
