import onnxruntime as ort
import numpy as np
import argparse

def main():
    # コマンドライン引数の設定
    parser = argparse.ArgumentParser(description="ONNX モデルにランダムな入力を与えて出力を確認するスクリプト")
    parser.add_argument("onnx_file", help="ONNX ファイルのパス") # 必須引数
    parser.add_argument("--seed", type=int, default=0, help="乱数シード (デフォルト: 0)") # オプション引数
    args = parser.parse_args()

    onnx_file = args.onnx_file
    seed = args.seed

    try:
        # ONNX モデルの読み込み
        ort_session = ort.InferenceSession(onnx_file)
    except Exception as e:
        print(f"Error loading ONNX file: {e}")
        return

    # 入力情報の取得 (形状も取得)
    input_details = ort_session.get_inputs()
    ort_inputs = {}

    if (seed != 0):
        np.random.seed(seed)

    for input in input_details:
        input_name = input.name
        input_shape = input.shape
        input_type = input.type

        # 形状が None を含む場合の処理 (可変長入力)
        input_shape = tuple(dim if isinstance(dim, int) else 1 for dim in input_shape)

        # ランダムな入力データの生成
        if str(input_type) == 'tensor(float)': # float型の場合
            input_data = np.random.rand(*input_shape).astype(np.float32)
        elif str(input_type) == 'tensor(int64)': # int64型の場合
            input_data = np.random.randint(0, 10, size=input_shape).astype(np.int64) # 例：0から9の整数
        # 他の型（int32, boolなど）が必要に応じて追加

        ort_inputs[input_name] = input_data

    # 推論の実行
    try:
        ort_outputs = ort_session.run(None, ort_inputs)
    except Exception as e:
        print(f"Error running inference: {e}")
        return

    # 出力の確認
    print(ort_outputs)

    # 各出力の確認 (必要に応じて)
    output_names = [output.name for output in ort_session.get_outputs()]
    for i, output in enumerate(ort_outputs):
        print(f"Output {output_names[i]}: {output}")

if __name__ == "__main__":
    main()
