
from bokeh.plotting import figure, output_file, show
from bokeh.models import ColumnDataSource, HoverTool
from bokeh.models.ranges import FactorRange
from bokeh.transform import factor_cmap
import math


def plots_with_error_bars(width, height, title, data_frame, color='#4080A0'):
    hover = HoverTool(tooltips=[
        ('name', '@ids'),
        ('mean', '@ys ± @error')
    ])
    p = figure(plot_height=height, plot_width=width, title=title, tools=[hover])

    ys = data_frame["mean"].values
    xs = list(range(len(ys)))
    ids = list(data_frame.index)
    yerrs = data_frame["error"].values

    # create the line start and coordinates for the error bars rendered using multi_line
    err_xs = []
    err_ys = []

    for x, y, yerr in zip(xs, ys, yerrs):
        err_xs.append((x, x))
        err_ys.append((y - yerr, y + yerr))

    source = {}
    source['xs'] = xs
    source['ys'] = ys
    source['ids'] = ids  # so we can include it in the hover tooltip.
    source['error'] = yerrs

    # plot them
    p.multi_line(err_xs, err_ys, color=color, line_width=0.5)
    c = p.circle('xs', 'ys', source=source, color=color, size=5, line_alpha=0)

    if p.y_range.start:
        p.y_range.start = min(p.y_range.start, math.floor(min(ys) / 10) * 10)
    else:
        p.y_range.start = math.floor(min(ys) / 10) * 10


    p.hover.renderers = [c]
    show(p)


def methods(obj):
    print('Methods:')
    print('\n'.join([x for x in dir(obj) if not x.startswith('_')]))
