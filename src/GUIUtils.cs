using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine;
using MelonLoader;

namespace DeliverySaver
{
    static class GUIUtils
    {
        public static void RebuildLayout(VerticalLayoutGroup vls)
        {
            // Ain't no way that's working in a single call but it does now
            LayoutRebuilder.ForceRebuildLayoutImmediate(vls.GetComponent<RectTransform>());
        }

        public static void RebuildLayout(GridLayoutGroup gls)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(gls.GetComponent<RectTransform>());
        }

        public static void RebuildLayout(HorizontalLayoutGroup hls)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(hls.GetComponent<RectTransform>());
        }
    }
}
