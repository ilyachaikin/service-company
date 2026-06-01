import React, { useState, useEffect } from 'react';
import { Form, Input, Button, Select, DatePicker, Card, message, Row, Col, Typography, Space, Alert } from 'antd';
import { useNavigate } from 'react-router-dom';
import api from '../../api/client';
import dayjs from 'dayjs';

const { Title } = Typography;

function parseApiErrors(err: any): string {
  const data = err?.response?.data;
  if (!data) return 'Не удалось создать заявку';

  if (data.errors && typeof data.errors === 'object') {
    const msgs = Object.values(data.errors).flat() as string[];
    if (msgs.length) return msgs.join('\n');
  }

  if (typeof data === 'string') return data;
  if (data.error) return data.error;

  if (data.detail) return data.detail;
  if (data.title) return data.title;

  return 'Не удалось создать заявку';
}

const TicketCreatePage: React.FC = () => {
  const [form] = Form.useForm();
  const navigate = useNavigate();
  const [clients, setClients] = useState([]);
  const [objects, setObjects] = useState([]);
  const [equipment, setEquipment] = useState([]);
  const [loading, setLoading] = useState(false);
  const [errorText, setErrorText] = useState<string | null>(null);

  useEffect(() => { fetchClients(); }, []);

  const fetchClients = async () => {
    const res = await api.get('/Clients', { params: { pageSize: 100 } });
    setClients(res.data.items);
  };

  const onClientChange = async (clientId: string) => {
    form.setFieldsValue({ serviceObjectId: undefined, equipmentId: undefined });
    const res = await api.get('/ServiceObjects', { params: { clientId, pageSize: 100 } });
    setObjects(res.data.items);
    setEquipment([]);
  };

  const onObjectChange = async (serviceObjectId: string) => {
    form.setFieldsValue({ equipmentId: undefined });
    const res = await api.get('/Equipments', { params: { serviceObjectId, pageSize: 100 } });
    setEquipment(res.data.items);
  };

  const onFinish = async (values: any) => {
    setLoading(true);
    setErrorText(null);
    try {
      await api.post('/Tickets', {
        ...values,
        dueDate: values.dueDate ? values.dueDate.toISOString() : null,
      });
      message.success('Заявка успешно создана');
      navigate('/tickets');
    } catch (err: any) {
      const text = parseApiErrors(err);
      setErrorText(text);

      window.scrollTo({ top: 0, behavior: 'smooth' });
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{ maxWidth: 800, margin: '0 auto', width: '100%' }}>
      <Title level={2}>Создать заявку</Title>

      {errorText && (
        <Alert
          type="error"
          showIcon
          closable
          style={{ marginBottom: 20, whiteSpace: 'pre-line' }}
          message="Ошибка при создании заявки"
          description={errorText}
          onClose={() => setErrorText(null)}
        />
      )}

      <Card>
        <Form form={form} layout="vertical" onFinish={onFinish} initialValues={{ priority: 2, type: 1 }}>
          <Form.Item
            name="title"
            label="Тема заявки"
            rules={[
              { required: true, message: 'Кратко опишите проблему (обязательное поле)' },
              { max: 200, message: 'Тема не может превышать 200 символов' },
            ]}
          >
            <Input placeholder="Например: Перегрев серверного шкафа" size="large" />
          </Form.Item>

          <Form.Item
            name="description"
            label="Подробное описание"
            rules={[{ required: true, message: 'Описание обязательно — укажите симптомы и обстоятельства' }]}
          >
            <Input.TextArea rows={4} placeholder="Опишите симптомы, коды ошибок и срочность..." />
          </Form.Item>

          <Row gutter={16}>
            <Col xs={24} sm={8}>
              <Form.Item name="priority" label="Приоритет">
                <Select>
                  <Select.Option value={0}>Критический</Select.Option>
                  <Select.Option value={1}>Высокий</Select.Option>
                  <Select.Option value={2}>Обычный</Select.Option>
                  <Select.Option value={3}>Низкий</Select.Option>
                </Select>
              </Form.Item>
            </Col>
            <Col xs={24} sm={8}>
              <Form.Item name="type" label="Тип">
                <Select>
                  <Select.Option value={0}>Аварийная</Select.Option>
                  <Select.Option value={1}>Плановая</Select.Option>
                  <Select.Option value={2}>Консультация</Select.Option>
                </Select>
              </Form.Item>
            </Col>
            <Col xs={24} sm={8}>
              <Form.Item
                name="dueDate"
                label="Срок исполнения"
                rules={[
                  {
                    validator: (_, value) => {
                      if (!value) return Promise.resolve();
                      if (dayjs(value).isAfter(dayjs())) return Promise.resolve();
                      return Promise.reject(new Error('Дата исполнения должна быть в будущем'));
                    },
                  },
                ]}
              >
                <DatePicker
                  style={{ width: '100%' }}
                  disabledDate={(d) => d && d.isBefore(dayjs().startOf('day'))}
                />
              </Form.Item>
            </Col>
          </Row>

          <SectionDivider>Объект и оборудование</SectionDivider>

          <Row gutter={16}>
            <Col span={24}>
              <Form.Item
                name="clientId"
                label="Клиент"
                rules={[{ required: true, message: 'Необходимо выбрать клиента' }]}
              >
                <Select placeholder="Выберите клиента" onChange={onClientChange} showSearch optionFilterProp="children">
                  {clients.map((c: any) => (
                    <Select.Option key={c.id} value={c.id}>{c.name} ({c.inn})</Select.Option>
                  ))}
                </Select>
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item name="serviceObjectId" label="Объект (адрес)">
                <Select
                  placeholder={objects.length ? 'Выберите объект' : 'Сначала выберите клиента'}
                  onChange={onObjectChange}
                  disabled={!objects.length}
                >
                  {objects.map((o: any) => <Select.Option key={o.id} value={o.id}>{o.name}</Select.Option>)}
                </Select>
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item name="equipmentId" label="Оборудование">
                <Select
                  placeholder={equipment.length ? 'Выберите оборудование' : 'Сначала выберите объект'}
                  disabled={!equipment.length}
                >
                  {equipment.map((e: any) => (
                    <Select.Option key={e.id} value={e.id}>{e.name} (С/Н: {e.serialNumber})</Select.Option>
                  ))}
                </Select>
              </Form.Item>
            </Col>
          </Row>

          <Form.Item style={{ marginTop: 24 }}>
            <Space>
              <Button type="primary" htmlType="submit" size="large" loading={loading}>
                Создать заявку
              </Button>
              <Button size="large" onClick={() => navigate('/tickets')}>
                Отмена
              </Button>
            </Space>
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
};

export default TicketCreatePage;

const SectionDivider = ({ children }: any) => (
  <div style={{ display: 'flex', alignItems: 'center', margin: '24px 0' }}>
    <div style={{ flex: 1, height: 1, backgroundColor: '#f0f0f0' }} />
    <span style={{ margin: '0 10px', fontSize: 13, color: '#999', textTransform: 'uppercase' }}>{children}</span>
    <div style={{ flex: 10, height: 1, backgroundColor: '#f0f0f0' }} />
  </div>
);
