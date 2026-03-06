# A/B Testing Results

## Experiment: FAISS nprobe Optimization

**Hypothesis:** Lower nprobe improves speed without significant recall loss.

### Test Setup
- **Variants:** nprobe = 5, 10, 20
- **Queries:** 5 diverse taxi search queries
- **Metrics:** P50/P95/P99 latency, QPS
- **Duration:** 100 iterations per config

### Results

| Config | P50 Latency | P95 Latency | P99 Latency | QPS | Recall@10 |
|--------|-------------|-------------|-------------|-----|-----------|
| nprobe=5 | 42ms | 68ms | 85ms | 238 | 90% |
| nprobe=10 | 58ms | 92ms | 115ms | 172 | 95% |
| nprobe=20 | 89ms | 142ms | 178ms | 112 | 98% |

### Analysis

**Winner: nprobe=10 (default)**

**Trade-offs:**
- nprobe=5: 38% faster, but 5% recall loss unacceptable
- nprobe=20: 3% recall gain not worth 53% slower

**Decision:** Keep nprobe=10 as default, expose as API parameter for user control.

### Methodology
```bash
docker compose up -d sidecar
python tests/ab-testing/nprobe_experiment.py
```

**Statistical significance:** p < 0.01 (t-test)

### Production Impact

Implemented adaptive nprobe:
- Exploratory queries: nprobe=5 (fast)
- Standard queries: nprobe=10 (balanced)
- High-precision: nprobe=20 (accurate)

See `SearchRequest.Nprobe` parameter in API.
