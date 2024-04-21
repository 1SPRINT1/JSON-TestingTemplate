using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ProductManager : MonoBehaviour
{
    public GameObject productPrefab; // Префаб для элемента списка
    public Transform productListContainer; // Родительский элемент для списка изделий

    // Стартовый метод, вызывается при запуске сцены
    void Start()
    {
        StartCoroutine(DownloadProductList());
    }

    public void OnSelectedProduct(string productID)
    {
        StartCoroutine(DownloadProductData(productID));
    }

    // Корутина для загрузки списка продуктов
    IEnumerator DownloadProductList()
    {
        string productListUrl = "https://variant-unity-test-server.vercel.app/api/list";
        UnityWebRequest request = UnityWebRequest.Get(productListUrl);
        yield return request.SendWebRequest();

        if (request.isNetworkError || request.isHttpError)
        {
            Debug.LogError(request.error);
        }
        else
        {
            // Преобразуем текст ответа в JSON объект
            string jsonResult = System.Text.Encoding.UTF8.GetString(request.downloadHandler.data);
            ProductList productList = JsonUtility.FromJson<ProductList>(jsonResult);

            // Создаём UI элементы для каждого изделия
            foreach (ProductItem item in productList.items)
            {
                // Создание нового экземпляра префаба для каждого изделия
               // GameObject newProduct = Instantiate(productPrefab, productListContainer);
               GameObject newProduct = Instantiate(productPrefab, productListContainer.transform);
                newProduct.GetComponentInChildren<TextMeshProUGUI>().text = item.name;

                // Загрузка иконки изделия
                StartCoroutine(DownloadIcon(item.icon, newProduct.GetComponentInChildren<Image>()));
            }
        }
    }

    // Корутина для загрузки иконки
    IEnumerator DownloadIcon(string iconUrl, Image iconImage)
    {
        UnityWebRequest iconRequest = UnityWebRequestTexture.GetTexture(iconUrl);
        yield return iconRequest.SendWebRequest();

        if (iconRequest.isNetworkError || iconRequest.isHttpError)
        {
            Debug.LogError(iconRequest.error);
        }
        else
        {
            // Устанавливаем скачанную иконку для UI элемента Image
            Texture2D iconTexture = DownloadHandlerTexture.GetContent(iconRequest);
            iconImage.sprite = Sprite.Create(iconTexture, new Rect(0, 0, iconTexture.width, iconTexture.height), new Vector2(0.5f, 0.5f));
        }
    }
    IEnumerator DownloadProductData(string productId)
    {
        string productDataUrl = "https://variant-unity-test-server.vercel.app/api/getObject?id=" + productId;
        UnityWebRequest request = UnityWebRequest.Get(productDataUrl);
        yield return request.SendWebRequest();

        if (request.isNetworkError || request.isHttpError)
        {
            Debug.LogError(request.error);
        }
        else
        {
            ProductData productData = JsonUtility.FromJson<ProductData>(request.downloadHandler.text);
        
            foreach (var objectData in productData.objects)
            {
                GameObject productPart = new GameObject("ProductPart");
                MeshFilter meshFilter = productPart.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = productPart.AddComponent<MeshRenderer>();

                // Создание и настройка меша
                Mesh mesh = new Mesh();
                mesh.vertices = ParseVector3Array(objectData.mesh.positions.Split(';'));
                mesh.normals = ParseVector3Array(objectData.mesh.normals.Split(';'));
                mesh.uv = ParseVector2Array(objectData.mesh.uvs.Split(';').ToString());
                mesh.triangles = ParseIntArray(objectData.mesh.indices.Split(';').ToString());

                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
                meshFilter.mesh = mesh;

                // TODO: Загрузить и применить материалы
                 StartCoroutine(ApplyMaterial(meshRenderer, objectData.material));

                // Установка трансформации изделия
                Vector3 position = StringToVector3(objectData.transform.position);
                Quaternion rotation = StringToQuaternion(objectData.transform.rotation);
                productPart.transform.localPosition = position;
                productPart.transform.localRotation = rotation;

                // TODO: добавьте изделие в иерархию сцены или установите его родителя, если необходимо
            }
        }
    }
    
    Vector3[] ParseVector3Array(string[] positionStrings)
    {
        Vector3[] vectors = new Vector3[positionStrings.Length];
        for (int i = 0; i < positionStrings.Length; i++)
        {
            string[] coords = positionStrings[i].Split(',');
            vectors[i] = new Vector3(
                float.Parse(coords[0]),
                float.Parse(coords[1]),
                float.Parse(coords[2])
            );
        }
        return vectors;
    }

    Vector2[] ParseVector2Array(string data)
    {
        
        string[] strings = data.Trim().Split(',');
        Vector2[] result = new Vector2[strings.Length / 2];
        for (int i = 0, j = 0; i < strings.Length; i += 2, j++)
        {
            result[j] = new Vector2(
                float.Parse(strings[i]),
                float.Parse(strings[i + 1]));
        }
        return result;
    }

    int[] ParseIntArray(string data)
    {
        string[] strings = data.Trim().Split(',');
        int[] result = new int[strings.Length];
        for (int i = 0; i < strings.Length; i++)
        {
            result[i] = int.Parse(strings[i]);
        }
        return result;
    }
    Vector3 StringToVector3(string s)
    {
        // Убедитесь, что строка не пуста
        if (string.IsNullOrEmpty(s))
            return Vector3.zero;
    
        // Удалите скобки (если присутствуют)
        s = s.Trim(new char[] { '(', ')' });

        // Разбейте значения, используя запятую в качестве разделителя
        string[] temp = s.Split(',');
        if (temp.Length != 3)
            throw new System.FormatException("Input string does not have the right format to be a Vector3: " + s);

        // Преобразуйте и верните вектор
        return new Vector3(
            float.Parse(temp[0]),
            float.Parse(temp[1]),
            float.Parse(temp[2]));
    }

    Quaternion StringToQuaternion(string s)
    {
        // Убедитесь, что строка не пуста
        if (string.IsNullOrEmpty(s))
            return Quaternion.identity;
    
        // Удалите скобки (если присутствуют)
        s = s.Trim(new char[] { '(', ')' });

        // Разбейте значения, используя запятую в качестве разделителя
        string[] temp = s.Split(',');
        if (temp.Length != 4)
            throw new System.FormatException("Input string does not have the right format to be a Quaternion: " + s);

        // Преобразуйте и верните кватернион
        return new Quaternion(
            float.Parse(temp[0]),
            float.Parse(temp[1]),
            float.Parse(temp[2]),
            float.Parse(temp[3]));
    }
    IEnumerator ApplyMaterial(MeshRenderer renderer, string materialData)
    {
        // Разбор JSON данных материала
        MaterialInfo materialInfo = JsonUtility.FromJson<MaterialInfo>(materialData);

        // Создание нового материала
        Material newMaterial = new Material(Shader.Find("Standard"));

        // Загрузка текстуры по URL
        if (!string.IsNullOrEmpty(materialInfo.textureUrl))
        {
            UnityWebRequest textureRequest = UnityWebRequestTexture.GetTexture(materialInfo.textureUrl);
            yield return textureRequest.SendWebRequest();
            if (textureRequest.isNetworkError || textureRequest.isHttpError)
            {
                Debug.LogError(textureRequest.error);
            }
            else
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(textureRequest);
                newMaterial.mainTexture = texture;
            }
        }

        // Применение цвета, если он указан
        if (materialInfo.color != null)
        {
            Color materialColor;
            if (ColorUtility.TryParseHtmlString(materialInfo.color, out materialColor))
            {
                newMaterial.color = materialColor;
            }
        }

        // Назначение нового материала рендереру
        renderer.material = newMaterial;
    }
    
}


// Класс для хранения списка изделий
[System.Serializable]
public class ProductList
{
    public List<ProductItem> items;
}

// Класс для хранения данных одного изделия
[System.Serializable]
public class ProductItem
{
    public string id;
    public string icon;
    public string name;
}
[System.Serializable]
public class ProductData
{
    public List<ObjectData> objects;
}

[System.Serializable]
public class ObjectData
{
    public TransformData transform;
    public MeshData mesh;
    public string material;
}

[System.Serializable]
public class TransformData
{
    public string position; // "x,y,z"
    public string rotation; // "x,y,z,w"
}

[System.Serializable]
public class MeshData
{
    public string positions; // "x1,y1,z1,x2,y2,z2,..."
    public string normals;   // "x1,y1,z1,x2,y2,z2,..."
    public string uvs;       // "u1,v1,u2,v2,..."
    public string indices;   // "0,1,2,3,4,5,..."
}
[System.Serializable]
public class MaterialInfo
{
    public string textureUrl;
    public string color; // В формате HTML (например, "#FF5733")
}