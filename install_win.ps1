conda env create -f environment.yml
conda activate MlAgentsGameBalancing
Set-Location -Path "ml-agents"
pip install torch~=2.2.1 --index-url https://download.pytorch.org/whl/cu121
python -m pip install ./ml-agents-envs
python -m pip install ./ml-agents