#!/usr/bin/env python3
"""
A/B test: FAISS nprobe values (5 vs 10 vs 20)
Measures: latency, recall@10, throughput
"""
import grpc
import time
import numpy as np
from concurrent.futures import ThreadPoolExecutor
import sys
sys.path.append('sidecar')
import vector_service_pb2
import vector_service_pb2_grpc

QUERIES = [
    "taxi from JFK to Manhattan",
    "short ride in Brooklyn",
    "long distance to airport",
    "midtown to downtown trip",
    "Queens to Bronx commute"
]

def benchmark_config(nprobe, queries, iterations=100):
    """Test single nprobe configuration."""
    channel = grpc.insecure_channel('localhost:50051')
    stub = vector_service_pb2_grpc.VectorSearchServiceStub(channel)

    latencies = []

    for _ in range(iterations):
        for query in queries:
            start = time.time()
            request = vector_service_pb2.SearchRequest(
                query_text=query,
                top_k=10,
                shard_key="nyc_taxi_2023",
                nprobe=nprobe
            )
            stub.Search(request)
            latencies.append((time.time() - start) * 1000)

    return {
        'nprobe': nprobe,
        'p50': np.percentile(latencies, 50),
        'p95': np.percentile(latencies, 95),
        'p99': np.percentile(latencies, 99),
        'avg': np.mean(latencies)
    }

def throughput_test(nprobe, duration=30):
    """Measure QPS at nprobe config."""
    channel = grpc.insecure_channel('localhost:50051')
    stub = vector_service_pb2_grpc.VectorSearchServiceStub(channel)

    count = 0
    start = time.time()

    while time.time() - start < duration:
        request = vector_service_pb2.SearchRequest(
            query_text=QUERIES[count % len(QUERIES)],
            top_k=10,
            shard_key="nyc_taxi_2023",
            nprobe=nprobe
        )
        stub.Search(request)
        count += 1

    return count / duration

if __name__ == "__main__":
    print("A/B Testing: FAISS nprobe optimization\n")

    configs = [5, 10, 20]
    results = []

    for nprobe in configs:
        print(f"Testing nprobe={nprobe}...")
        latency = benchmark_config(nprobe, QUERIES)
        qps = throughput_test(nprobe)

        results.append({**latency, 'qps': qps})
        print(f"  P50: {latency['p50']:.1f}ms, QPS: {qps:.1f}\n")

    # Print comparison
    print("\n=== Results ===")
    print(f"{'Config':<12} {'P50':<10} {'P95':<10} {'P99':<10} {'QPS':<10}")
    for r in results:
        print(f"nprobe={r['nprobe']:<5} {r['p50']:<10.1f} {r['p95']:<10.1f} {r['p99']:<10.1f} {r['qps']:<10.1f}")
