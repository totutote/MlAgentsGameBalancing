#!/bin/bash

conda env create -f environment.yml
conda activate MlAgentsGameBalancing
cd ml-agents
pip3 install torch~=2.2.1 --index-url https://download.pytorch.org/whl/cu121
python -m pip install ./ml-agents-envs
python -m pip install ./ml-agents
