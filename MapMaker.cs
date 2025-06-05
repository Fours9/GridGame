using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Скрипт отвечает за генерацию воксельной карты с ландшафтом, пещерами, дорогами и сохранением данных по каждому блоку.
public class Main : MonoBehaviour
{

    public List<GridCellBehaviour> allCells = new List<GridCellBehaviour>();


    // Размер карты по ширине и высоте
    public int width = 32;
    public int height = 32;
    public int mapHeight = 12; // Высота карты (количество блоков по высоте)

    // Масштабы для разных слоёв шума (используются для генерации ландшафта)
    public float scale1 = 3f;
    public float scale2 = 4f;
    public float scale3 = 5f;
    public float scale4 = 5f;

    // Пороговые значения для генерации различных типов блоков
    public float thresholdG = 0.5f;     // Порог для зелёных клеток
    public float thresholdH = 0.6f;     // Не используется (можно удалить или использовать)
    public float thresholdM = 0.63f;    // Порог для красных клеток (гор)
    public float caveThreshold = 0.3f;  // Порог для генерации пещер
    public float thresholdBlue = 0.7f;  // Порог для генерации озёр

    // Префабы для разных типов блоков
    public GameObject greenCube;
    public GameObject yellowCube;
    public GameObject redCube;
    public GameObject purpleCube;  // Пещеры или смешанные блоки
    public GameObject grayCube;    // Дороги
    public GameObject blueCube;    // Озёра
    public GameObject brownCube;   // Дороги, затопленные водой
    public GameObject stoneCube;   // Камень внутри пещер
    public GameObject gridCellPrefab; // Универсальный префаб (может использоваться отдельно)
    public GameObject stoneRoadCube;  // <--- Новый префаб для StoneRoad

    // Флаги включения каждой из 4 дорог
    public bool enableNorthRoad = true;
    public bool enableSouthRoad = true;
    public bool enableWestRoad = true;
    public bool enableEastRoad = true;

    // Насколько сильно изгибаются дороги (больше значение — сильнее волнистость)
    [Range(0f, 10f)]
    public float roadCurviness = 4f;

    // Смещения для генерации шумов (Perlin Noise)
    private float offset1X, offset1Z;
    private float offset2X, offset2Z;
    private float offset3X, offset3Z;
    private float offset4X, offset4Z;

    // Типы клеток на карте
    public enum CellType { StoneRoad, Stone, None, Green, Yellow, Red, Purple, Gray, Blue, Brown }

    public MoveCell[,,] GetCellData() => CellData;


    // Структура, описывающая информацию об одной клетке
    [System.Serializable]
    public struct CellInfo
    {
        public int x, y, z;     // Координаты клетки
        public CellType type;   // Тип клетки

        public CellInfo(int x, int y, int z, CellType type)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.type = type;
        }
    }


    public CellInfo[,,] CellArray;
    public MoveCell[,,] CellData;

    // Метод, вызывается при старте сцены
    void Start()
    {
        // Генерируем случайные смещения для Perlin Noise
        offset1X = Random.Range(0f, 9999f);
        offset1Z = Random.Range(0f, 9999f);
        offset2X = Random.Range(0f, 9999f);
        offset2Z = Random.Range(0f, 9999f);
        offset3X = Random.Range(0f, 9999f);
        offset3Z = Random.Range(0f, 9999f);
        offset4X = Random.Range(0f, 9999f);
        offset4Z = Random.Range(0f, 9999f);

        // Запускаем генерацию карты
        Generate3DMap();
        ClearGridCells();  // Очищаем предыдущие клетки GridCell
        GridCellGenerator(); // Генерация клеток для перемещения (GridCell)
    }

    // Метод генерации 3D карты (ландшафта, пещер, дорог)
    void Generate3DMap()
    {
        CellArray = new CellInfo[width, mapHeight, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                for (int z = 0; z < height; z++)
                {
                    CellArray[x, y, z] = new CellInfo(x, y, z, CellType.None);
                }
            }
        }

        // Создаём двумерные массивы типов клеток, маски пещер и озёр
        CellType[,] map = new CellType[width, height];  // Основная карта с типами клеток
        bool[,] isCave = new bool[width, height];  // Массив для отметки пещер
        bool[,] blueMask = new bool[width, height];  // Массив для маски озёр

        // Генерация шумов и первичной карты
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                // Нормализуем координаты для шума
                float nx = (float)x / width;
                float nz = (float)z / height;

                // Получаем значения шумов
                float noise1 = Mathf.PerlinNoise(nx * scale1 + offset1X, nz * scale1 + offset1Z);
                float noise2 = Mathf.PerlinNoise(nx * scale2 + offset2X, nz * scale2 + offset2Z);
                float noise3 = Mathf.PerlinNoise(nx * scale3 + offset3X, nz * scale3 + offset3Z);
                float noise4 = Mathf.PerlinNoise(nx * scale4 + offset4X, nz * scale4 + offset4Z);

                // Определяем тип местности по значениям шумов
                if (noise2 > thresholdM)
                    map[x, z] = CellType.Red; // Горы
                else if (noise1 < thresholdG)
                    map[x, z] = CellType.Green; // Равнина
                else
                    map[x, z] = CellType.Yellow; // Холмы

                // Генерация пещер по третьему шуму
                if (noise3 < caveThreshold)
                {
                    // Снижение ландшафта при наличии пещеры
                    if (map[x, z] == CellType.Yellow)
                        map[x, z] = CellType.Green;
                    else if (map[x, z] == CellType.Red)
                    {
                        // Проверка на соседние красные клетки
                        bool hasNearbyRed = false;
                        for (int i = 0; i < 4; i++)
                        {
                            int nx2 = x + (i == 0 ? -1 : i == 1 ? 1 : 0);
                            int nz2 = z + (i == 2 ? -1 : i == 3 ? 1 : 0);
                            if (nx2 >= 0 && nx2 < width && nz2 >= 0 && nz2 < height)
                            {
                                if (map[nx2, nz2] == CellType.Red)
                                {
                                    hasNearbyRed = true;
                                    break;
                                }
                            }
                        }
                        // Если рядом есть красная — превращаем в пещеру
                        if (hasNearbyRed)
                            map[x, z] = CellType.Purple;
                    }

                    // Отмечаем клетку как пещеру
                    isCave[x, z] = true;
                }

                // Генерация маски озёр
                if (noise4 > thresholdBlue)
                    blueMask[x, z] = true;
            }
        }

        // Объединение пещер в области и переработка их вида
        // Создаётся двумерный массив, который хранит информацию о том, была ли клетка уже обработана
        bool[,] visited = new bool[width, height];

        // Задаются смещения по x и z для обхода четырёх направлений: влево, вправо, вверх, вниз
        int[] dx = { -1, 1, 0, 0 };
        int[] dz = { 0, 0, -1, 1 };

        // Проходим по каждой строке карты (ось Z)
        for (int z = 0; z < height; z++)
        {
            // Проходим по каждой колонке карты (ось X)
            for (int x = 0; x < width; x++)
            {
                // Пропускаем клетку, если:
                // 1. Она не является пещерой
                // 2. Уже была обработана ранее
                if (!isCave[x, z] || visited[x, z])
                    continue;

                // Создаём список клеток, которые входят в текущую компоненту пещеры
                List<Vector2Int> component = new List<Vector2Int>();

                // Кол-во соседних клеток типа Red (например, горы)
                int redAdjacentCount = 0;

                // Периметр данной компоненты (используется для оценки окружения)
                int perimeter = 0;

                // Очередь для BFS-обхода клеток пещеры
                Queue<Vector2Int> queue = new Queue<Vector2Int>();

                // Добавляем текущую клетку в очередь и помечаем как посещённую
                queue.Enqueue(new Vector2Int(x, z));
                visited[x, z] = true;

                // Начинаем BFS-обход (поиск в ширину)
                while (queue.Count > 0)
                {
                    // Извлекаем клетку из очереди
                    Vector2Int current = queue.Dequeue();

                    // Добавляем её в компоненту пещеры
                    component.Add(current);

                    // Обходим все 4 соседние клетки (влево, вправо, вверх, вниз)
                    for (int i = 0; i < 4; i++)
                    {
                        // Вычисляем координаты соседа
                        int nx = current.x + dx[i];
                        int nz = current.y + dz[i];

                        // Проверяем, что сосед находится в пределах карты
                        if (nx >= 0 && nx < width && nz >= 0 && nz < height)
                        {
                            // Если сосед имеет тип Red — увеличиваем счётчик
                            if (map[nx, nz] == CellType.Red)
                                redAdjacentCount++;

                            // Если сосед также является пещерой и ещё не посещён
                            if (isCave[nx, nz] && !visited[nx, nz])
                            {
                                // Помечаем его как посещённый и добавляем в очередь
                                visited[nx, nz] = true;
                                queue.Enqueue(new Vector2Int(nx, nz));
                            }
                        }
                        else
                        {
                            // Если сосед за пределами карты — считаем его частью периметра
                            perimeter++;
                        }
                    }
                }

                // Дополнительно учитываем периметр как 4 стороны каждой клетки
                perimeter += component.Count * 4;

                // Определяем, достаточно ли клетка окружена Red-блоками (порог — 20% периметра)
                bool enoughRedTouch = redAdjacentCount >= (perimeter / 5);

                // Если да — окрашиваем как Purple (например, фиолетовая пещера), иначе — как Green
                CellType finalType = enoughRedTouch ? CellType.Purple : CellType.Green;

                // Назначаем тип всем клеткам компоненты
                foreach (var pos in component)
                {
                    map[pos.x, pos.y] = finalType;
                }
            }
        }

        int centerX = width / 2;
        int centerZ = height / 2;

        // Дорога с Севера (top → center)
        if (enableNorthRoad)
        {
            for (int z = height - 1; z >= centerZ; z--)
            {
                float noiseOffset = Mathf.PerlinNoise(42.42f, z * 0.15f) * roadCurviness - (roadCurviness / 2f);
                int x = Mathf.Clamp(Mathf.RoundToInt(centerX + noiseOffset), 0, width - 1);

                for (int bx = -1; bx <= 0; bx++)
                {
                    int xx = x + bx;
                    if (xx >= 0 && xx < width)
                    {
                        if (map[xx, z] == CellType.Red)
                            map[xx, z] = CellType.Purple;
                        else
                            map[xx, z] = CellType.Gray;
                    }
                }
            }
        }

        // Дорога с Юга (bottom → center)
        if (enableSouthRoad)
        {
            for (int z = 0; z <= centerZ; z++)
            {
                float noiseOffset = Mathf.PerlinNoise(99.99f, z * 0.15f) * roadCurviness - (roadCurviness / 2f);
                int x = Mathf.Clamp(Mathf.RoundToInt(centerX + noiseOffset), 0, width - 1);

                for (int bx = -1; bx <= 0; bx++)
                {
                    int xx = x + bx;
                    if (xx >= 0 && xx < width)
                    {
                        if (map[xx, z] == CellType.Red)
                            map[xx, z] = CellType.Purple;
                        else
                            map[xx, z] = CellType.Gray;
                    }
                }
            }
        }

        // Дорога с Запада (left → center)
        if (enableWestRoad)
        {
            for (int x = 0; x <= centerX; x++)
            {
                float noiseOffset = Mathf.PerlinNoise(x * 0.15f, 123.45f) * roadCurviness - (roadCurviness / 2f);
                int z = Mathf.Clamp(Mathf.RoundToInt(centerZ + noiseOffset), 0, height - 1);

                for (int bz = -1; bz <= 0; bz++)
                {
                    int zz = z + bz;
                    if (zz >= 0 && zz < height)
                    {
                        if (map[x, zz] == CellType.Red)
                            map[x, zz] = CellType.Purple;
                        else
                            map[x, zz] = CellType.Gray;
                    }
                }
            }
        }

        // Дорога с Востока (right → center)
        if (enableEastRoad)
        {
            for (int x = width - 1; x >= centerX; x--)
            {
                float noiseOffset = Mathf.PerlinNoise(x * 0.15f, 888.88f) * roadCurviness - (roadCurviness / 2f);
                int z = Mathf.Clamp(Mathf.RoundToInt(centerZ + noiseOffset), 0, height - 1);

                for (int bz = -1; bz <= 0; bz++)
                {
                    int zz = z + bz;
                    if (zz >= 0 && zz < height)
                    {
                        if (map[x, zz] == CellType.Red)
                            map[x, zz] = CellType.Purple;
                        else
                            map[x, zz] = CellType.Gray;
                    }
                }
            }
        }

        // Проход по всей карте для наложения синих (Blue) и коричневых (Brown) масок
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                // Проверка маски: нужно ли перекрасить клетку
                if (blueMask[x, z])
                {
                    // Если клетка была зелёной — превращается в воду
                    if (map[x, z] == CellType.Green)
                        map[x, z] = CellType.Blue;

                    // Если клетка была дорогой — превращается в болото
                    else if (map[x, z] == CellType.Gray)
                        map[x, z] = CellType.Brown;
                }
            }
        }

        // Финальный этап — визуализация карты: создание блоков и заполнение массива `generatedCells`
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                // Получаем тип клетки из карты
                CellType type = map[x, z];

                // Переменные для хранения префаба и высоты блока
                GameObject mainPrefab = null;
                int heightBlocks = 1;

                // Подбор подходящего префаба и высоты в зависимости от типа клетки
                switch (type)
                {
                    case CellType.Green:
                        mainPrefab = greenCube;
                        heightBlocks = 2;
                        break;

                    case CellType.Yellow:
                        mainPrefab = yellowCube;
                        heightBlocks = 3;
                        break;

                    case CellType.Red:
                        mainPrefab = redCube;
                        heightBlocks = 4;
                        break;

                    case CellType.Gray:
                        mainPrefab = grayCube;
                        heightBlocks = 2;
                        break;

                    case CellType.Blue:
                        mainPrefab = blueCube;
                        heightBlocks = 1;
                        break;

                    case CellType.Brown:
                        mainPrefab = brownCube;
                        heightBlocks = 2;
                        break;
                }

                if (type == CellType.Purple)
                {
                    // -- ВНИМАНИЕ: добавляем логику для StoneRoad --
                    // Проверяем, какой тип блока был под этим местом ДО превращения в Purple (Gray, Brown, etc.)
                    bool wasRoad = false;
                    // Проверяем, была ли здесь дорога или болото
                    if (mainPrefab == grayCube || mainPrefab == brownCube)
                        wasRoad = true;

                    for (int y = 0; y < 2; y++)
                    {
                        if (wasRoad && stoneRoadCube != null)
                        {
                            Instantiate(stoneRoadCube, new Vector3(x, y, z), Quaternion.identity, transform);
                            CellArray[x, y, z] = new CellInfo(x, y, z, CellType.StoneRoad);
                        }
                        else
                        {
                            Instantiate(stoneCube, new Vector3(x, y, z), Quaternion.identity, transform);
                            CellArray[x, y, z] = new CellInfo(x, y, z, CellType.Stone);
                        }
                    }

                    // Пустота (воздух) в центре
                    for (int y = 2; y < 4; y++)
                    {
                        CellArray[x, y, z] = new CellInfo(x, y, z, CellType.None);
                    }
                    // Верхний слой — фиолетовый блок
                    Instantiate(purpleCube, new Vector3(x, 4, z), Quaternion.identity, transform);
                    CellArray[x, 4, z] = new CellInfo(x, 4, z, CellType.Purple);

                    // Верхняя часть пустая до y = 8
                    for (int y = 5; y < mapHeight; y++)
                    {
                        CellArray[x, y, z] = new CellInfo(x, y, z, CellType.None);
                    }
                }
                else if (mainPrefab != null)
                {
                    // Обычные блоки: отрисовка основного блока
                    for (int y = 0; y < heightBlocks; y++)
                    {
                        Instantiate(mainPrefab, new Vector3(x, y, z), Quaternion.identity, transform);
                        CellArray[x, y, z] = new CellInfo(x, y, z, type);
                    }

                    // Пустые блоки сверху до уровня 8
                    for (int y = heightBlocks; y < mapHeight; y++)
                    {
                        CellArray[x, y, z] = new CellInfo(x, y, z, CellType.None);
                    }
                }
                else
                {
                    // Полностью пустая клетка (например, None)
                    for (int y = 0; y < mapHeight; y++)
                    {
                        CellArray[x, y, z] = new CellInfo(x, y, z, CellType.None);
                    }
                }
            }
        }
    }

    // Метод для генерации клеток перемещения (GridCell) в пустых местах над твердыми блоками
    void GridCellGenerator()
    {
        CellData = new MoveCell[width, mapHeight, height]; // Инициализируем массив для хранения логических клеток перемещения

        // Добавление GridCell в пустые ячейки над твердыми блоками
        for (int x = 0; x < width; x++)                    // Перебор всех координат по ширине
        {
            for (int z = 0; z < height; z++)               // Перебор всех координат по глубине (оси Z)
            {
                for (int y = 1; y < mapHeight - 1; y++)                // Начинаем с y=1, чтобы y-1 не вышел за пределы
                {
                    // Находим текущую клетку по координатам
                    CellInfo current = CellArray[x, y, z];

                    // Находим клетку, которая находится непосредственно под текущей
                    CellInfo below = CellArray[x, y - 1, z];

                    CellInfo upper = CellArray[x, y + 1, z]; // Клетка над текущей

                    // Если текущая клетка пуста, а под ней есть твёрдый блок (но не вода)
                    if (current.type == CellType.None && below.type != CellType.None && below.type != CellType.Blue && upper.type == CellType.None)
                    {
                        // Создаём визуальный объект клетки в сцене
                        GameObject cellObj = Instantiate(gridCellPrefab, new Vector3(x, y, z), Quaternion.identity, transform);

                        // Создаём логическую клетку перемещения и добавляем в список доступных для пути
                        MoveCell moveCell = new MoveCell(x, y, z, cellObj, true, 1, below.type, null);
                        cellObj.GetComponent<GridCellBehaviour>().myCell = moveCell; // <-- Исправлено здесь!
                        CellData[x, y, z] = moveCell; // Сохраняем ссылку на MoveCell в массиве CellData

                        // --- Добавь вот это: ---
                        GridCellBehaviour gridCell = cellObj.GetComponent<GridCellBehaviour>();
                        if (gridCell != null)
                        {
                            allCells.Add(gridCell);
                        }
                    }
                }
            }
        }
    }

    // Метод для очистки всех клеток GridCell из сцены и сброса данных
    public void ClearGridCells()
    {
        // Перебираем всех потомков текущего объекта (transform)
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);

            // Проверяем, был ли объект создан из gridCellPrefab
            if (child.name.Contains(gridCellPrefab.name))
            {
                Destroy(child.gameObject);
            }
        }

        // Также очищаем вспомогательные структуры
        CellData = new MoveCell[width, mapHeight, height]; // Можно оставить нули
    }

    public void HighlightReachableCells(List<MoveCell> cells)
    {
        // 1. Сбросить у всех GridCellBehaviour (не белый, а исходный цвет!)
        foreach (var cell in CellData)
        {
            if (cell != null && cell.CellObject != null)
            {
                var behaviour = cell.CellObject.GetComponent<GridCellBehaviour>();
                if (behaviour != null)
                    behaviour.SetReachableHighlight(false);
            }
        }

        // 2. Теперь отметить доступные как reachable
        if (cells == null) return;
        foreach (var cell in cells)
        {
            if (cell != null && cell.CellObject != null)
            {
                var behaviour = cell.CellObject.GetComponent<GridCellBehaviour>();
                if (behaviour != null)
                    behaviour.SetReachableHighlight(true);
            }
        }
    }

    public bool IsCellInBounds(Vector3Int pos)
    {
        return pos.x >= 0 && pos.x < width
            && pos.y >= 0 && pos.y < mapHeight
            && pos.z >= 0 && pos.z < height;
    }

    public MoveCell GetCurrentUnitCell()
    {
        var unit = InitiativeManager.Instance.GetCurrentUnit();
        if (unit == null) return null;
        Vector3Int c = unit.CurrentCell;
        if (!IsCellInBounds(c)) return null;
        return CellData[c.x, c.y, c.z];
    }
}