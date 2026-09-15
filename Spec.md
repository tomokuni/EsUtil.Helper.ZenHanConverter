# ZenHanConverter ソースコード詳細仕様書

## 概要
- 全角/半角変換を中心とする文字列変換ユーティリティ群。
- CSV 定義からコード生成された静的クラス群と、変換エントリ/変換リストを扱う汎用クラスで構成されます。
- `ConvertMethod` による複合変換 API で正規化から幅変換まで一括適用できます。

## アーキテクチャ概要
- 変換定義は `ConvertDefine.csv` → T4 (`Generated.tt`) で `Generated.cs` に自動生成され、カテゴリ別静的アクセサを提供。
- コアロジックは `ConvertPairs`/`EntryRecord` レコードで共通化し、キャッシュと不変コレクションでスレッドセーフに高速化。
- 複合メソッド群 (`ConvertMethod.cs`) は生成済みの `GroupOf` クラス経由で `ConvertPairs` を連結して高レベル API を構成。

## 主な型とメンバー
### `ZenHanConverter` (in `ConvertMethod.cs`)
- パブリックな静的ユーティリティクラス。
- アプリケーションが直接利用する高レベル API (`ToHan`, `ToZen...` 等) を提供。

### `Generated.cs` 内の型
- `Define`: 全定義リスト (`AllList`) などを保持。
- `GroupOf`: `Ascii`, `Kana` などのカテゴリ別グループへのアクセサを提供。
- `GroupOf_Ascii` / `GroupOf_Kana`: 各カテゴリ内の変換マップ (`ToHanMap`, `ToZenMap` 等) をプロパティとして公開。
- `NameOf`: 定義されている個々のエントリ名へのアクセサ。

### `ZenHanConverter` に定義された複合 API (public static)
- `ToNormalize(string text)`: 特殊空白 (→U+0020)・各種ダッシュ (→U+002D) を正規化し、分離した全角カナを合成。半角カナ同士 (`ｶ`+`ﾞ`) は対象外。
- `ToHan(string text)`: 数字/英字/記号/カナ（長音/濁点含む）を半角へ統一。
- `ToZenWithKatakana(string text)`: 全角化し、半角カナを全角カタカナへ統一（ひらがなは変換しない）。
- `ToZenWithHiragana(string text)`: 全角化し、半角カナを全角ひらがなへ統一（全角カタカナは変換しない）。
- `ToHanOnlyAscii(string text)`: 数字/英字/記号のみ半角化。
- `ToZenOnlyAscii(string text)`: 数字/英字/記号のみ全角化（半角スペースは全角スペース U+3000 へ）。
- `ToHanOnlyKana(string text)`: ひらがな/カタカナとかな記号を半角化（長音 `ー` は半角長音 `ｰ` へ）。
- `ToHanOnlyKatakana(string text)`: カタカナとかな記号のみ半角化（ひらがなは変換しない）。
- `ToZenOnlyKatakana(string text)`: 半角カナとかな記号を全角カタカナへ統一。
- `ToZenKatakanaOnlyKana(string text)`: ひらがなと半角カナを全角カタカナへ統一。
- `ToZenHiraganaOnlyKana(string text)`: カタカナ（全角/半角）をひらがなへ統一。
- `ToUpperCase(string text)`: 全角/半角の英字を大文字へ。
- `ToLowerCase(string text)`: 全角/半角の英字を小文字へ。
- `ConvertTabToSpace(string text)`: タブを半角スペースへ。
- `ConvertBackslashToHanYen(string text)`: バックスラッシュを半角円記号へ。

※ `ToHan`・`ToZen` 系と `ToUpper`/`ToLowerCase` は内部で `ToNormalize` を適用する。

### `ConvertPairs` 系
- 役割: `(Source, Target)` ペアを保持し、Regex を用いた置換や連鎖/統合を提供。
- コンストラクタ: `IEnumerable<(string Source, string Target)>` 版と `params` で複数集合を平坦化する版。
- 列挙: `IEnumerable<(string, string)>` を実装し、`GetEnumerator` でペア列挙を提供。
- 連鎖/統合: `Chain(ConvertPairs, bool includeUnmatchedFirst, bool includeUnmatchedSecond)` で Target と Source を突合し連鎖。`Chain()` は未マッチ切り捨て、`ChainMerge()` は未マッチも保持。
- 変換実行: `Convert(string text)` はキャッシュ済み Regex/辞書で置換。Regex 生成不可時は空文字列を返却。null 入力は空文字扱い。
- 静的生成: `FromForward` / `FromInverse` / `FromFunc` 等で `EntryRecord` 一覧や遅延ロード関数からインスタンス化。
- 内部フィールド: `_cacheRegexMapDictionary` (`ConcurrentDictionary<ConvertPairs, (Regex regex, Dictionary<string, string> map)>`) で Regex/辞書をキャッシュ。

### `EntryRecord`
- プロパティ: `Category`, `Group`, `SubGroup`, `Forward`, `Inverse`, `Source`, `Target`, `Name`, `Summary`。
- コンストラクタ: 文字列9要素版とタプル版。`U+XXXX` をデコードし、null を空文字へフォールバック。
- 暗黙変換: `(string, ...)` タプルから `EntryRecord` へ。
- デコンストラクタ: `(Source, Target)` 版と全プロパティ版を提供。
- 静的メソッド: `GetEntryList(key)` で階層キー (`ZenHanConverter|...`) に応じた一覧を取得・キャッシュ。
- 内部フィールド: `_cacheEntryListMap` (`ConcurrentDictionary<string, ImmutableList<EntryRecord>>`) でフィルタ結果をキャッシュ。

## 定義済みエントリ一覧 (GroupOf / NameOf)
`GroupOf` クラスおよび `NameOf` クラスでアクセス可能な定義一覧です。
`GroupOf.{カテゴリ}.{グループ}` で `ConvertPairs` を、`NameOf.{カテゴリ}.{定義名}` で個別の定義を取得できます。
いずれも `EsUtil.Helper.ZenHanConverter` 名前空間に属します。

### `GroupOf` の階層

各ノードには変換方向ごとの `ConvertPairs` プロパティ (`ToHanMap` / `ToZenMap` / `ToUpperMap` / `ToLowerMap` / `ComposeMap` / `ToAsciiMap` / `FromAsciiMap` / `ToHiraMap` / `ToKataMap` / `ToASpaceMap` / `ToAHyphenMap` / `ToSpaceFromTabMap` / `ToYenFromBslashMap` / `ToBslashFromYenMap`) が定義されています。

| パス | 内容 |
| --- | --- |
| `GroupOf.Ascii` | 英数字・記号の全定義 |
| `GroupOf.Ascii.Numeric.Number` | 数字 `０`～`９` ↔ `0`～`9` |
| `GroupOf.Ascii.Alphabet` | 英字の全定義 |
| `GroupOf.Ascii.Alphabet.Han` / `.Zen` | 半角英字 / 全角英字の大文字⇔小文字 |
| `GroupOf.Ascii.Alphabet.Large` / `.Small` | 英大文字 / 英小文字の全角⇔半角 |
| `GroupOf.Ascii.Symbol` | 記号の全定義（34 パターン） |
| `GroupOf.Ascii.Symbol.Bracket` / `.Fin` / `.Ope` / `.Punc` | 括弧 / 末尾記号 / 演算子 / 句読点 |
| `GroupOf.Ascii.Replace` | 置換・正規化 |
| `GroupOf.Ascii.Replace.Fringe` | 特殊空白・各種ダッシュの正規化 |
| `GroupOf.Ascii.Replace.Han` / `.Zen` | 半角円記号 / 全角円記号とバックスラッシュの相互変換 |
| `GroupOf.Kana` | かなの全定義 |
| `GroupOf.Kana.Kata` | カタカナ（81 パターン） |
| `GroupOf.Kana.Kata.Large` / `.Small` | カタカナ 清音/濁音/半濁音 / 小書き文字 |
| `GroupOf.Kana.Kata.ZZ` / `.ZH` / `.HZ` | 分離カタカナの合成（全-全 / 全-半 / 半-全） |
| `GroupOf.Kana.Hira` | ひらがな |
| `GroupOf.Kana.Hira.Large` / `.Small` | ひらがな 清音/濁音/半濁音 / 小書き文字 |
| `GroupOf.Kana.Hira.ZZ` / `.ZH` | 分離ひらがなの合成（全-全 / 全-半） |
| `GroupOf.Kana.Symbol` | かな記号 |
| `GroupOf.Kana.Symbol.Voice` / `.Han` / `.Zen` / `.Punc` | 濁点・半濁点 / 半角かな記号 / 全角かな記号 / 句読点 |
| `GroupOf.Kana.Case` | カタカナ⇔ひらがな (`ToHiraMap` / `ToKataMap`) |
| `GroupOf.Kana.Case.Large` / `.Small` | かな 清音等 / 小書き文字 |

### `NameOf` の階層

| パス | 内容 |
| --- | --- |
| `NameOf.Ascii.n0`～`n9` | 数字 `０`～`９` |
| `NameOf.Ascii.A`～`Z` | 英字。`.Large`(英大文字の全⇔半)、`.Small`(英小文字の全⇔半)、`.Han`(半角の大⇔小)、`.Zen`(全角の大⇔小) |
| `NameOf.Ascii.ParenthesisLeft` ほか | 記号（後述の記号名一覧を参照） |
| `NameOf.Ascii.Space.Symbol` / `.Replace` | スペース |
| `NameOf.Ascii.Bslash.Symbol` / `.Replace.Han` / `.Replace.Zen` | バックスラッシュ |
| `NameOf.Ascii.Yen.Symbol` / `.Replace.Han` / `.Replace.Zen` | 円記号 |
| `NameOf.Ascii.Hyphen` / `.Tab` | 各種ダッシュ / タブ |
| `NameOf.Kana.{A, I, U, E, O, KA, KI, ..., N, GA, ..., VU}` | かな。`.Kata`(カタカナ)、`.Hira`(ひらがな)、`.Case`(カタカナ⇔ひらがな) |
| `NameOf.Kana.Voice` / `.SemiVoice` | 濁点 `゛` / 半濁点 `゜` |
| `NameOf.Kana.MiddleDot` | 中点 `・` |
| `NameOf.Kana.Prolong.Voice` / `.Han` / `.Zen` | 長音 `ー` / `ｰ` |
| `NameOf.Kana.LeftCornerBracket` / `.RightCornerBracket` | かぎ括弧 `「` / `」` |
| `NameOf.Kana.Period.Punc` / `.Han` / `.Zen` | 句点 `。` / `｡` |
| `NameOf.Kana.Comma.Punc` / `.Han` / `.Zen` | 読点 `、` / `､` |

### `NameOf.Ascii` の記号名一覧

| 定義名 | 内容 |
| --- | --- |
| `ParenthesisLeft` / `ParenthesisRight` | 丸括弧 左 / 右 |
| `SquareBracketLeft` / `SquareBracketRight` | 角括弧 左 / 右 |
| `CurlyBracketLeft` / `CurlyBracketRight` | 波括弧 左 / 右 |
| `DoubleQuote` / `SingleQuote` / `Backquote` | ダブルクォート / シングルクォート / バッククォート |
| `Comma` / `Period` / `Colon` / `Semicolon` | カンマ / ピリオド / コロン / セミコロン |
| `LessThan` / `GreaterThan` / `Equal` | 不等号 小 / 不等号 大 / 等号 |
| `Plus` / `HyphenMinus` / `Tilde` / `Slash` | プラス / ハイフン / チルダ / スラッシュ |
| `Question` / `Exclamation` | はてな / 感嘆符 |
| `Sharp` / `Dollar` / `Percent` | シャープ / ドル / パーセント |
| `Ampersand` / `Asterisk` / `At` | アンパサンド / アスタリスク / アットマーク |
| `Caret` / `UnderBar` / `VerticalBar` | キャレット / アンダーバー / 縦棒 |

## 変換定義のカバレッジ
- 数値: `０`～`９` ↔ `0`～`9`。
- 英字: 全角大文字/小文字と半角大文字/小文字を相互変換。ケース変換対応。
- 記号: 括弧、クォート、区切り記号、算術/比較記号、`￥`記号等。
- カタカナ/ひらがな: 清音/濁音/半濁音/小書き/長音/中点/句読点/カギ括弧を全角/半角で相互変換。
- 分離カナ合成: `カ`+`゛` などの分離形を合成ルール (`GroupOf.Kana.Kata.ComposeMap` / `GroupOf.Kana.Hira.ComposeMap`) で正規化。全角+全角 / 全角+半角が対象。
- フリンジ空白・ダッシュ: ノーブレークスペースや各種ダッシュを正規化 (`GroupOf.Ascii.Replace.Fringe`)。

## 内部実装ポリシー
- 文字列比較は `StringComparer.Ordinal` を原則使用。
- Regex 生成不可時は変換を行わず空文字を返し、例外を回避。
- 重複キーは「先勝ち」で決定性を確保。
- 不変コレクションによる共有で副作用を排除し、スレッドセーフを維持。
- `ConvertPairs.Chain` で第2段を Source ごとにグルーピングし O(1) 判定で連結。

## パフォーマンス向上施策
- **Regex/辞書キャッシュ**: `ConcurrentDictionary` でコンパイル済み Regex とマッピングをキャッシュ。
- **先勝ち辞書化**: 同一 Source を一度だけ登録し、無駄な上書きと処理時間を防止。
- **Unicode デコードの先行**: `EntryRecord` 生成時に `U+XXXX` 表記をデコードし、実行時オーバーヘッドを削減。
- **null/空の早期スキップ**: 変換不要ケースを早期 return。
- **不変リストのスライス展開**: `[..]` 構文による効率的なリスト構築。
- **連鎖処理の辞書化**: `ConvertPairs` 連鎖時に第2段を辞書化 (`GroupBy` + `ToDictionary`) し、結合の線形走査を削減。

## リリースビルドでのベンチマーク結果

1000 文字程度の混合テキスト（英数字・仮名・記号）を 10,000 回ループ処理した際の実測値です（.NET 8 / Release）。

| メソッド | 実行時間 (10,000回合計) | 1回あたりの平均 |
| --- | --- | --- |
| `ToNormalize` | 131 ms | 13.1 µs |
| `ToHan` | 1585 ms | 158.5 µs |
| `ToZenWithKatakana` | 179 ms | 17.9 µs |

- `ToHan` は変換パターンが最多 (`GroupOf.Ascii` + `GroupOf.Kana` 全域) のため相対的に遅いが、10k 文字/ms 級のスループットを確保。
- `ToNormalize` と `ToZen` 系は `ConvertPairs` のキャッシュが効き、数 µs オーダーで完了する。

## 注意事項
- 生成物 (`Generated.cs`) は CSV 変更時に再生成が必要。
- 新規カテゴリや種別を追加する場合、CSV・T4 テンプレートの両方を更新し、テストを拡張する。
- 変換定義の優先度は定義順に依存するため、追加順序に留意。
