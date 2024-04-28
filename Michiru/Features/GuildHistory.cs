using ScottPlot;

namespace Michiru.Features;

// https://scottplot.net/quickstart/console/
// https://scottplot.net/cookbook/5.0/
// https://scottplot.net/faq/dependencies/

public class GuildHistory {
    public static Plot DataPlot() {
        var plot = new Plot();

        var hm1 = plot.Add.Heatmap(TestHeatMap());
        hm1.Colormap = new ScottPlot.Colormaps.Turbo();

        plot.XLabel("Days", 6f);
        plot.YLabel("Hours", 23f);
        // plot.Add.ColorBar(hm1);

        plot.SavePng("result.png", 400, 300);
        return plot;
    }

    public static double[] xDays = [ 0, 1, 2, 3, 4, 5, 6 ];
    public static double[] yHours = [];

    public static double[,] TestHeatMap() {
        return new double[,] {
            { },
            { },
            { },
            { },
            { },
            { },
            { }
        };
    }
}