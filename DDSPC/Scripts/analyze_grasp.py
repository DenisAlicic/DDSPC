import json
import statistics
import matplotlib.pyplot as plt
import pandas as pd
from pathlib import Path

def load_results(experiment_path):
    results = []
    for result_file in Path(experiment_path).glob('**/*_GRASP.json'):
        with open(result_file) as f:
            data = json.load(f)
            # Konvertuj Runtime iz TimeSpan stringa u sekunde
            if 'Runtime' in data:
                try:
                    # Za .NET TimeSpan format "HH:MM:SS.FFFFFFF"
                    parts = data['Runtime'].split(':')
                    if len(parts) == 3:
                        hours = int(parts[0])
                        minutes = int(parts[1])
                        seconds = float(parts[2])
                        data['RuntimeSeconds'] = hours * 3600 + minutes * 60 + seconds
                    else:
                        data['RuntimeSeconds'] = 0.0
                except:
                    data['RuntimeSeconds'] = 0.0
            data['GraphName'] = result_file.stem.replace('_GRASP', '')
            results.append(data)
    return results

def analyze(results):
    df = pd.DataFrame(results)
    
    # Osnovne metrike
    metrics = {
        'gap_stats': {
            'min': df['GapPercent'].min(),
            'max': df['GapPercent'].max(),
            'mean': df['GapPercent'].mean(),
            'median': df['GapPercent'].median(),
            'std': df['GapPercent'].std()
        },
        'runtime_by_size': df.groupby('NumNodes')['RuntimeSeconds'].mean().to_dict(),
        'feasibility_rate': len(df[df['Value'] < float('inf')]) / len(df)
    }
    
    # Konvergencija (ako postoji iteration log)
    if 'IterationLog' in df.columns:
        plot_convergence(df)
    
    return metrics

def plot_convergence(df):
    plt.figure(figsize=(10, 6))
    for _, row in df.iterrows():
        if isinstance(row['IterationLog'], list):
            iterations = [x['Item1'] for x in row['IterationLog']]
            values = [x['Item2'] for x in row['IterationLog']]
            plt.plot(iterations, values, alpha=0.4)
    
    plt.title('GRASP Convergence Patterns')
    plt.xlabel('Iteration')
    plt.ylabel('Objective Value')
    plt.savefig('grasp_convergence.png')
    plt.close()

if __name__ == "__main__":
    import sys
    experiment_path = sys.argv[1] if len(sys.argv) > 1 else '../Data/Experiments/Latest'
    results = load_results(experiment_path)
    metrics = analyze(results)
    
    with open('grasp_metrics.json', 'w') as f:
        json.dump(metrics, f, indent=2)
    
    print("Analysis complete. Results saved to grasp_metrics.json")