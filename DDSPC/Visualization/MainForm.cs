using DDSPC.Data;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using Color = Microsoft.Msagl.Drawing.Color;
using Edge = Microsoft.Msagl.Drawing.Edge;
using FontStyle = System.Drawing.FontStyle;
using Node = Microsoft.Msagl.Drawing.Node;

namespace DDSPC.Visualization;

public class MainForm : Form
{
    private GViewer viewer;
    private int numNodes;
    private StatusStrip statusStrip;
    private ToolStripStatusLabel statusLabel;
    private List<(int, int)> edges;
    private List<(int, int)> conflicts;
    private HashSet<int>? D1;
    private HashSet<int>? D2;
    private int? solutionValue;
    private string? solutionSource;

    public MainForm(DDSPCInput input, DDSPCOutput? output)
    {
        numNodes = input.NumNodes;
        edges = input.Edges;
        conflicts = input.Conflicts;
        D1 = output?.D1;
        D2 = output?.D2;
        solutionValue = output?.Value;
        solutionSource = output?.Solver;
        InitializeComponent();
        Load += MainForm_Load;
    }

    private void InitializeComponent()
    {
        Text = "Graph Visualization";
        ClientSize = new Size(800, 600);

        viewer = new GViewer();
        viewer.Dock = DockStyle.Fill;

        statusStrip = new StatusStrip();
        statusLabel = new ToolStripStatusLabel
        {
            Text = "Solution: Not calculated",
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };
        statusStrip.Items.Add(statusLabel);
        statusStrip.Dock = DockStyle.Bottom;

        // Postavi kontrolu
        Controls.Add(viewer);
        Controls.Add(statusStrip);
    }

    private void MainForm_Load(object sender, EventArgs e)
    {
        Graph graph = new Graph("Graf")
        {
            Directed = false
        };

        for (int i = 0; i < numNodes; i++)
        {
            Node? node = graph.AddNode(i.ToString());

            if (D1 != null && D1.Contains(i))
            {
                node.Attr.FillColor = Color.Blue;
                node.LabelText = $"{i}";
                node.Attr.Shape = Shape.Circle;
            }
            else if (D2 != null && D2.Contains(i))
            {
                node.Attr.FillColor = Color.Green;
                node.LabelText = $"{i}";
                node.Attr.Shape = Shape.Circle;
            }
            else
            {
                node.Attr.FillColor = Color.Gray;
                node.LabelText = $"{i}";
                node.Attr.Shape = Shape.Circle;
            }
        }

        foreach ((int u, int v) in edges)
        {
            Edge? edge = graph.AddEdge(u.ToString(), v.ToString());
            edge.Attr.ArrowheadAtSource = ArrowStyle.None;
            edge.Attr.ArrowheadAtTarget = ArrowStyle.None;
            edge.Attr.LineWidth = 1;
        }

        foreach ((int m, int n) in conflicts)
        {
            Edge? edge = graph.AddEdge(m.ToString(), n.ToString());
            edge.Attr.Color = Color.Red;
            edge.Attr.ArrowheadAtSource = ArrowStyle.None;
            edge.Attr.ArrowheadAtTarget = ArrowStyle.None;
            edge.Attr.AddStyle(Style.Dashed);
            edge.Attr.LineWidth = 2;
        }

        viewer.Graph = graph;

        if (solutionValue.HasValue)
        {
            statusLabel.Text =
                $"Solution: |D1| = {D1?.Count ?? 0}, |D2| = {D2?.Count ?? 0}, Total = {solutionValue}, Solver = {solutionSource}";
        }
    }
}