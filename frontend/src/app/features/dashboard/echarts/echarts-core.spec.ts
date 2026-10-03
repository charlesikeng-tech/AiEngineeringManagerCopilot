import { setPlatformAPI, use } from 'echarts/core';
import { SVGRenderer } from 'echarts/renderers';

describe('dashboard ECharts loader', () => {
  it('registers the line chart and axes for the dynamically loaded module', async () => {
    const { init } = await import('./echarts-core');
    use([SVGRenderer]);
    // SVG SSR still measures labels; this test does not need a browser canvas.
    setPlatformAPI({
      measureText: (text) => ({ width: text.length * 8 }),
    });

    const chart = init(null, undefined, {
      renderer: 'svg',
      ssr: true,
      width: 400,
      height: 200,
    });

    try {
      chart.setOption({
        xAxis: { type: 'category', data: ['Aug', 'Sep'] },
        yAxis: { type: 'value', min: 0, max: 100 },
        tooltip: { trigger: 'axis' },
        series: [
          {
            type: 'line',
            data: [60, 80],
            lineStyle: { color: '#4f46e5' },
          },
        ],
      });

      const svg = chart.renderToSVGString();
      expect(svg).toContain('<svg');
      expect(svg).toContain('stroke="#4f46e5"');
      expect(chart.getOption()['series']).toEqual([
        expect.objectContaining({ type: 'line', data: [60, 80] }),
      ]);
    } finally {
      chart.dispose();
    }
  });
});
