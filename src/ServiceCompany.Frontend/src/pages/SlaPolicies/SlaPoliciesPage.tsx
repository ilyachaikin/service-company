import React, { useState, useEffect } from 'react';
import { Table, Button, Modal, Form, Input, InputNumber, message, Card, Typography, Space, Tag, Popconfirm } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, SafetyCertificateOutlined } from '@ant-design/icons';
import api from '../../api/client';

const { Title, Text } = Typography;

const SlaPoliciesPage: React.FC = () => {
  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(false);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingPolicy, setEditingPolicy] = useState<any>(null);
  const [form] = Form.useForm();

  const fetchPolicies = async () => {
    setLoading(true);
    try {
      const res = await api.get('/SlaPolicies');
      setData(res.data);
    } catch {
      message.error('Не удалось загрузить SLA-политики');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchPolicies(); }, []);

  const openCreate = () => {
    setEditingPolicy(null);
    form.resetFields();
    setIsModalOpen(true);
  };

  const openEdit = (record: any) => {
    setEditingPolicy(record);
    form.setFieldsValue({
      name: record.name,
      description: record.description,
      responseTimeHours: record.responseTimeHours,
      resolutionTimeHours: record.resolutionTimeHours,
    });
    setIsModalOpen(true);
  };

  const handleSave = async (values: any) => {
    try {
      if (editingPolicy) {
        await api.put(`/SlaPolicies/${editingPolicy.id}`, { ...values, id: editingPolicy.id });
        message.success('SLA-политика обновлена');
      } else {
        await api.post('/SlaPolicies', values);
        message.success('SLA-политика создана');
      }
      setIsModalOpen(false);
      form.resetFields();
      setEditingPolicy(null);
      fetchPolicies();
    } catch (err: any) {
      const errMsg = err.response?.data?.errors
        ? Object.values(err.response.data.errors).flat().join('; ')
        : err.response?.data?.error || 'Ошибка сохранения';
      message.error(errMsg);
    }
  };

  const handleDelete = async (id: string) => {
    try {
      await api.delete(`/SlaPolicies/${id}`);
      message.success('SLA-политика удалена');
      fetchPolicies();
    } catch {
      message.error('Не удалось удалить SLA-политику');
    }
  };

  const columns = [
    {
      title: 'Наименование',
      dataIndex: 'name',
      key: 'name',
      render: (text: string) => (
        <Space>
          <SafetyCertificateOutlined style={{ color: '#1890ff' }} />
          <Text strong>{text}</Text>
        </Space>
      )
    },
    {
      title: 'Описание',
      dataIndex: 'description',
      key: 'description',
      render: (text: string) => text || <Text type="secondary">—</Text>
    },
    {
      title: 'Время ответа',
      dataIndex: 'responseTimeHours',
      key: 'responseTimeHours',
      render: (h: number) => <Tag color="blue">{h} ч</Tag>
    },
    {
      title: 'Время решения',
      dataIndex: 'resolutionTimeHours',
      key: 'resolutionTimeHours',
      render: (h: number) => <Tag color="green">{h} ч</Tag>
    },
    {
      title: 'Действия',
      key: 'actions',
      render: (_: any, record: any) => (
        <Space>
          <Button type="text" icon={<EditOutlined />} onClick={() => openEdit(record)} />
          <Popconfirm
            title="Удалить SLA-политику?"
            description="Убедитесь, что политика не используется в договорах."
            onConfirm={() => handleDelete(record.id)}
            okText="Удалить"
            cancelText="Отмена"
            okButtonProps={{ danger: true }}
          >
            <Button type="text" danger icon={<DeleteOutlined />} />
          </Popconfirm>
        </Space>
      )
    }
  ];

  return (
    <div>
      <div className="page-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <Title level={2} style={{ margin: 0 }}>SLA-политики</Title>
        <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>
          Добавить политику
        </Button>
      </div>

      <Card>
        <Table
          columns={columns}
          dataSource={data}
          loading={loading}
          rowKey="id"
          pagination={false}
          scroll={{ x: 600 }}
        />
      </Card>

      <Modal
        title={editingPolicy ? 'Редактировать SLA-политику' : 'Новая SLA-политика'}
        open={isModalOpen}
        onCancel={() => { setIsModalOpen(false); setEditingPolicy(null); form.resetFields(); }}
        onOk={() => form.submit()}
        okText={editingPolicy ? 'Сохранить' : 'Создать'}
        cancelText="Отмена"
        width="min(560px, 95vw)"
      >
        <Form form={form} layout="vertical" onFinish={handleSave}>
          <Form.Item
            name="name"
            label="Наименование"
            rules={[
              { required: true, message: 'Введите наименование SLA-политики' },
              { max: 100, message: 'Не более 100 символов' }
            ]}
          >
            <Input placeholder="Например: Стандарт, Приоритет, VIP" />
          </Form.Item>

          <Form.Item name="description" label="Описание">
            <Input.TextArea rows={3} placeholder="Краткое описание условий SLA" />
          </Form.Item>

          <Space size="large" style={{ width: '100%' }} wrap>
            <Form.Item
              name="responseTimeHours"
              label="Время реакции (часов)"
              rules={[
                { required: true, message: 'Укажите время реакции' },
                { type: 'number', min: 1, message: 'Минимум 1 час' }
              ]}
              style={{ flex: 1 }}
            >
              <InputNumber style={{ width: '100%' }} min={1} placeholder="4" addonAfter="ч" />
            </Form.Item>

            <Form.Item
              name="resolutionTimeHours"
              label="Время решения (часов)"
              rules={[
                { required: true, message: 'Укажите время решения' },
                { type: 'number', min: 1, message: 'Минимум 1 час' },
                ({ getFieldValue }) => ({
                  validator(_, value) {
                    if (!value || value >= getFieldValue('responseTimeHours')) {
                      return Promise.resolve();
                    }
                    return Promise.reject(new Error('Время решения не может быть меньше времени реакции'));
                  },
                }),
              ]}
              style={{ flex: 1 }}
            >
              <InputNumber style={{ width: '100%' }} min={1} placeholder="24" addonAfter="ч" />
            </Form.Item>
          </Space>

          <Card size="small" style={{ backgroundColor: '#f6ffed', borderColor: '#b7eb8f' }}>
            <Text type="secondary">
              <b>Время реакции</b> — максимальное время от регистрации заявки до начала работ.<br />
              <b>Время решения</b> — максимальное время на полное устранение неисправности.
            </Text>
          </Card>
        </Form>
      </Modal>
    </div>
  );
};

export default SlaPoliciesPage;
