#!/bin/bash
# -- REAL PARAMETERS OF TARGET JOB TO SUBMIT
#SBATCH --job-name=dummy_test
#SBATCH --account=ATR-25-1
#SBATCH --partition=qcpu
#SBATCH --nodes=400
#SBATCH --ntasks-per-node=128
#SBATCH --time=00:01:00
#SBATCH --output=dummy_%j.out
#SBATCH --error=dummy_%j.err

# Print job information
echo "Job started at: $(date)"
echo "Running on nodes: $SLURM_JOB_NODELIST"
echo "Number of nodes: $SLURM_JOB_NUM_NODES"
echo "Total tasks: $SLURM_NTASKS"

# Dummy work - just sleep and print from each task
srun bash -c 'echo "Task $SLURM_PROCID on node $(hostname) sleeping..."; sleep 300; echo "Task $SLURM_PROCID finished"'

echo "Job finished at: $(date)"
# Expected to be run only with: sbatch --test-only dummy_job.sh
