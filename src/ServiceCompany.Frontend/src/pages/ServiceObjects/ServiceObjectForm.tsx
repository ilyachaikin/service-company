import React, { useEffect, useState } from 'react';
import { Modal, Form, Input, Switch, message, Select, Row, Col, Alert, Tag, Button } from 'antd';
import { EnvironmentOutlined, LoadingOutlined, CheckCircleOutlined } from '@ant-design/icons';
import api from '../../api/client';

interface ServiceObjectFormProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  initialData?: any;
}

type GeoState = 'idle' | 'loading' | 'found' | 'error';

const ServiceObjectForm: React.FC<ServiceObjectFormProps> = ({ isOpen, onClose, onSuccess, initialData }) => {
  const [form] = Form.useForm();
  const [clients, setClients] = useState<any[]>([]);
  const [geoState, setGeoState] = useState<GeoState>('idle');

  const [coords, setCoords] = useState<{ lat: number; lng: number } | null>(null);

  useEffect(() => {
    if (isOpen) {
      fetchClients();
      if (initialData) {
        form.setFieldsValue(initialData);

        if (initialData.latitude != null && initialData.longitude != null) {
          setCoords({ lat: initialData.latitude, lng: initialData.longitude });
          setGeoState('found');
        } else {
          setCoords(null);
          setGeoState('idle');
        }
      } else {
        form.resetFields();
        setCoords(null);
        setGeoState('idle');
      }
    }
  }, [isOpen, initialData, form]);

  const fetchClients = async () => {
    const res = await api.get('/Clients', { params: { pageSize: 100 } });
    setClients(res.data.items);
  };

  const KARAGANDA_VIEWBOX = '68.0,51.5,79.5,45.5';
  const KARAGANDA_LAT_MIN = 45.5, KARAGANDA_LAT_MAX = 51.5;
  const KARAGANDA_LON_MIN = 68.0, KARAGANDA_LON_MAX = 79.5;

  const geocode = async (address: string): Promise<{ lat: number; lng: number } | null> => {
    try {

      const cityQuery = encodeURIComponent(`${address}, Караганда, Казахстан`);
      const res = await fetch(
        `https://nominatim.openstreetmap.org/search?q=${cityQuery}&format=json&limit=5` +
        `&viewbox=${KARAGANDA_VIEWBOX}&bounded=1&accept-language=ru`,
        { headers: { 'Accept-Language': 'ru' } }
      );
      const data = await res.json();
      if (data.length > 0) {
        const lat = parseFloat(data[0].lat);
        const lon = parseFloat(data[0].lon);

        if (lat >= KARAGANDA_LAT_MIN && lat <= KARAGANDA_LAT_MAX &&
            lon >= KARAGANDA_LON_MIN && lon <= KARAGANDA_LON_MAX) {
          return { lat, lng: lon };
        }
      }

      const oblastQuery = encodeURIComponent(`${address}, Карагандинская область, Казахстан`);
      const res2 = await fetch(
        `https://nominatim.openstreetmap.org/search?q=${oblastQuery}&format=json&limit=5` +
        `&viewbox=${KARAGANDA_VIEWBOX}&bounded=1&accept-language=ru`,
        { headers: { 'Accept-Language': 'ru' } }
      );
      const data2 = await res2.json();
      if (data2.length > 0) {
        const lat = parseFloat(data2[0].lat);
        const lon = parseFloat(data2[0].lon);
        if (lat >= KARAGANDA_LAT_MIN && lat <= KARAGANDA_LAT_MAX &&
            lon >= KARAGANDA_LON_MIN && lon <= KARAGANDA_LON_MAX) {
          return { lat, lng: lon };
        }
      }
      return null;
    } catch {
      return null;
    }
  };

  const handleFindOnMap = async () => {
    const address = form.getFieldValue('address');
    if (!address?.trim()) {
      message.warning('Введите адрес для поиска');
      return;
    }
    setGeoState('loading');
    const result = await geocode(address);
    if (result) {
      setCoords(result);
      setGeoState('found');
      message.success('Местоположение найдено!');
    } else {
      setCoords(null);
      setGeoState('error');
      message.warning('Адрес не найден на карте. Проверьте правильность написания.');
    }
  };

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();

      let finalCoords = coords;
      if (!finalCoords && values.address?.trim()) {
        setGeoState('loading');
        finalCoords = await geocode(values.address);
        if (finalCoords) {
          setCoords(finalCoords);
          setGeoState('found');
        } else {
          setGeoState('error');
        }
      }

      const payload = {
        ...values,
        latitude:  finalCoords?.lat ?? null,
        longitude: finalCoords?.lng ?? null,
      };

      if (initialData) {
        await api.put(`/ServiceObjects/${initialData.id}`, { ...payload, id: initialData.id });
        message.success('Объект обновлён');
      } else {
        await api.post('/ServiceObjects', payload);
        message.success('Объект создан');
      }
      onSuccess();
    } catch (error: any) {
      if (error?.errorFields) return;
      message.error(error.response?.data?.error || 'Ошибка при сохранении');
    }
  };

  const geoStatusEl = (() => {
    if (geoState === 'loading') return <Tag icon={<LoadingOutlined spin />} color="processing">Определение координат...</Tag>;
    if (geoState === 'found' && coords)
      return <Tag icon={<CheckCircleOutlined />} color="success">
        Координаты: {coords.lat.toFixed(4)}°N, {coords.lng.toFixed(4)}°E
      </Tag>;
    if (geoState === 'error') return <Tag color="warning">Координаты не найдены — объект не отобразится на карте</Tag>;
    return null;
  })();

  return (
    <Modal
      title={initialData ? 'Редактировать объект' : 'Добавить объект'}
      open={isOpen}
      onCancel={onClose}
      onOk={handleSubmit}
      okText={initialData ? 'Сохранить' : 'Создать'}
      cancelText="Отмена"
      width={680}
      confirmLoading={geoState === 'loading'}
    >
      <Form form={form} layout="vertical" initialValues={{ isActive: true }}>
        <Row gutter={16}>
          <Col span={24}>
            <Form.Item name="clientId" label="Клиент" rules={[{ required: true, message: 'Выберите клиента' }]}>
              <Select placeholder="Выберите клиента" disabled={!!initialData}>
                {clients.map(c => <Select.Option key={c.id} value={c.id}>{c.name}</Select.Option>)}
              </Select>
            </Form.Item>
          </Col>
          <Col span={24}>
            <Form.Item name="name" label="Наименование объекта" rules={[{ required: true, message: 'Введите наименование' }]}>
              <Input placeholder="Например: Главный офис" />
            </Form.Item>
          </Col>
          <Col span={24}>
            <Form.Item
              name="address"
              label="Адрес"
              rules={[{ required: true, message: 'Введите адрес объекта' }]}
              help={
                <span style={{ fontSize: 12, color: '#888' }}>
                  Введите адрес в Карагандe или Карагандинской обл., затем нажмите «Найти на карте»
                </span>
              }
            >
              <Input.TextArea
                placeholder="Например: ул. Ерубаева, 45 или Темиртау, ул. Ленина, 10"
                rows={2}
                onChange={() => {

                  if (geoState === 'found' || geoState === 'error') {
                    setGeoState('idle');
                    setCoords(null);
                  }
                }}
              />
            </Form.Item>
          </Col>
          <Col span={24} style={{ marginBottom: 12 }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 12, flexWrap: 'wrap' }}>
              <Button
                icon={<EnvironmentOutlined />}
                onClick={handleFindOnMap}
                loading={geoState === 'loading'}
              >
                Найти на карте
              </Button>
              {geoStatusEl}
            </div>
          </Col>
          {geoState === 'error' && (
            <Col span={24}>
              <Alert
                type="warning"
                showIcon
                message="Адрес не распознан"
                description="Объект будет сохранён без привязки к карте. Попробуйте уточнить адрес (добавьте город, район или индекс)."
                style={{ marginBottom: 12 }}
              />
            </Col>
          )}
          <Col span={24}>
            <Form.Item name="description" label="Внутренние заметки">
              <Input.TextArea placeholder="Коды доступа, инструкции для входа..." rows={3} />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item name="isActive" label="Активен" valuePropName="checked">
              <Switch />
            </Form.Item>
          </Col>
        </Row>
      </Form>
    </Modal>
  );
};

export default ServiceObjectForm;
