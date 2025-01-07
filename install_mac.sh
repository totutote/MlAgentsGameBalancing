#!/bin/bash

conda env create -f environment.yml
conda activate MlAgentsGameBalancing
cd ml-agents
pip3 install grpcio
python -m pip install ./ml-agents-envs
python -m pip install ./ml-agents
